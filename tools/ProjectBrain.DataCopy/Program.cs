// One-off copy of users and their data from the old Azure SQL database into Neon Postgres.
// See README.md. Rehearses (copies inside a transaction, then rolls back) unless --apply is passed.

using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Npgsql;
using NpgsqlTypes;

var apply = args.Contains("--apply");

var sourceConnectionString = RequireEnv("SOURCE_CONNECTION_STRING");
var targetConnectionString = RequireEnv("TARGET_CONNECTION_STRING");

var targetBuilder = new NpgsqlConnectionStringBuilder(targetConnectionString) { CommandTimeout = 600, IncludeErrorDetail = true };
Console.WriteLine($"Target: {targetBuilder.Host} / {targetBuilder.Database}");
if (targetBuilder.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase) == true)
{
    Console.WriteLine("  warning: this is a pooled Neon host. Use the direct (non -pooler) connection string for bulk loads.");
}
Console.WriteLine(apply ? "Mode: APPLY (changes are committed)" : "Mode: rehearsal (everything is rolled back; pass --apply to commit)");
Console.WriteLine();

await using var source = await OpenSourceAsync(sourceConnectionString);
await using var target = new NpgsqlConnection(targetBuilder.ConnectionString);
await target.OpenAsync();

var schema = await TargetSchema.LoadAsync(target);
var sourceColumns = await LoadSourceColumnsAsync(source);

// --- Classify tables ------------------------------------------------------------------------

var userTables = Plan.UserTables(schema);
var referenceTables = schema.Tables.Where(t => !userTables.Contains(t)).ToHashSet();
var copyMissingTables = Plan.CopyMissing.Where(schema.Tables.Contains).ToList();
var remapTables = Plan.RemapByKey.Keys.Where(schema.Tables.Contains).ToList();

var loadOrder = schema.TopologicalOrder(userTables);
Console.WriteLine($"User data ({userTables.Count} tables, load order):");
Console.WriteLine("  " + string.Join(", ", loadOrder));
Console.WriteLine($"Reference data, kept as it is in the target ({referenceTables.Count} tables):");
Console.WriteLine("  " + string.Join(", ", referenceTables.Order()));
Console.WriteLine($"  matched by natural key: {string.Join(", ", remapTables.Select(t => $"{t}.{Plan.RemapByKey[t]}"))}");
Console.WriteLine($"  rows missing from the target are copied: {string.Join(", ", copyMissingTables)}");
Console.WriteLine();

var problems = new List<string>();

await using var tx = await target.BeginTransactionAsync();
try
{

// --- Reference data -------------------------------------------------------------------------

// Seeded tables get fresh GUIDs in each database, so map source Id -> target Id via the natural key.
var idMaps = new Dictionary<string, Dictionary<object, object>>();
foreach (var table in remapTables.Where(sourceColumns.ContainsKey))
{
    var key = Plan.RemapByKey[table];
    var sourceIds = await ReadSourcePairsAsync(source, table, "Id", key);
    var targetIds = await ReadTargetPairsAsync(target, tx, table, "Id", key);
    var targetByKey = targetIds.ToDictionary(p => p.Value, p => p.Key);

    var map = new Dictionary<object, object>();
    var missing = new List<object>();
    foreach (var (id, naturalKey) in sourceIds)
    {
        if (targetByKey.TryGetValue(naturalKey, out var targetId)) map[id] = targetId;
        else missing.Add(id);
    }
    idMaps[table] = map;

    Console.WriteLine($"{table}: {map.Count} matched by {key}, {missing.Count} only in source (copied with their source Id)");
    if (missing.Count > 0)
    {
        await CopyRowsAsync(source, target, schema, sourceColumns, table, problems, idMaps, onlyIds: missing.ToHashSet());
    }
}

foreach (var table in schema.TopologicalOrder(copyMissingTables.Where(sourceColumns.ContainsKey).ToHashSet()))
{
    var sourceIds = (await ReadSourcePairsAsync(source, table, "Id", "Id")).Select(p => p.Key).ToHashSet();
    var targetIds = (await ReadTargetPairsAsync(target, tx, table, "Id", "Id")).Select(p => p.Key).ToHashSet();
    var missing = sourceIds.Where(id => !targetIds.Contains(id)).ToHashSet();
    Console.WriteLine($"{table}: {missing.Count} of {sourceIds.Count} source rows missing from the target, copying them");
    if (missing.Count > 0)
    {
        await CopyRowsAsync(source, target, schema, sourceColumns, table, problems, idMaps, onlyIds: missing);
    }
}
Console.WriteLine();

// --- Replace user data ----------------------------------------------------------------------

Console.WriteLine("Deleting user data from the target:");
foreach (var table in Enumerable.Reverse(loadOrder))
{
    await using var delete = new NpgsqlCommand($"DELETE FROM {Pg(table)}", target, tx);
    var deleted = await delete.ExecuteNonQueryAsync();
    if (deleted > 0) Console.WriteLine($"  {table}: {deleted}");
}
Console.WriteLine();

Console.WriteLine("Copying user data:");
var copied = new Dictionary<string, long>();
foreach (var table in loadOrder)
{
    copied[table] = await CopyRowsAsync(source, target, schema, sourceColumns, table, problems, idMaps);
}
Console.WriteLine();

await ResetSequencesAsync(target, tx, schema, loadOrder.Concat(copyMissingTables).Concat(remapTables));

// --- Verify -------------------------------------------------------------------------------

foreach (var table in loadOrder)
{
    await using var count = new NpgsqlCommand($"SELECT COUNT(*) FROM {Pg(table)}", target, tx);
    var targetCount = (long)(await count.ExecuteScalarAsync())!;
    if (targetCount != copied[table]) problems.Add($"{table}: copied {copied[table]} rows but the target has {targetCount}");
}

if (problems.Count > 0)
{
    Console.WriteLine("Problems:");
    foreach (var p in problems) Console.WriteLine($"  - {p}");
    Console.WriteLine();
}

var total = copied.Values.Sum();
if (apply && problems.Count == 0)
{
    await tx.CommitAsync();
    Console.WriteLine($"Committed. {total} user rows copied.");
}
else
{
    await tx.RollbackAsync();
    Console.WriteLine(apply
        ? $"Rolled back because of the problems above. Nothing was changed."
        : $"Rehearsal finished: {total} user rows would be copied{(problems.Count > 0 ? $", with {problems.Count} problem(s) to fix first" : "")}. Rolled back; nothing was changed.");
}
return problems.Count == 0 ? 0 : 1;
}
catch (CopyAbortedException)
{
    // A failed COPY aborts the Postgres transaction, so stop here.
    await tx.RollbackAsync();
    Console.WriteLine();
    Console.WriteLine("Stopped. Problems:");
    foreach (var p in problems) Console.WriteLine($"  - {p}");
    Console.WriteLine("Rolled back; nothing was changed.");
    return 1;
}

// --- Helpers ---------------------------------------------------------------------------------

static string RequireEnv(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Set the {name} environment variable (see README.md).");

static string Pg(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

static string Sql(string identifier) => "[" + identifier.Replace("]", "]]") + "]";

static async Task<SqlConnection> OpenSourceAsync(string connectionString)
{
    var builder = new SqlConnectionStringBuilder(connectionString);
    var connection = new SqlConnection(builder.ConnectionString);

    // Azure SQL here is Entra-only. Without a password or Authentication keyword, sign in as the
    // `az login` user (or any other DefaultAzureCredential source).
    if (string.IsNullOrEmpty(builder.Password)
        && builder.Authentication == SqlAuthenticationMethod.NotSpecified
        && builder.DataSource.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase))
    {
        var token = await new DefaultAzureCredential().GetTokenAsync(
            new TokenRequestContext(["https://database.windows.net/.default"]));
        connection.AccessToken = token.Token;
    }

    await connection.OpenAsync();
    Console.WriteLine($"Source: {builder.DataSource} / {builder.InitialCatalog}");
    return connection;
}

static async Task<Dictionary<string, Dictionary<string, string>>> LoadSourceColumnsAsync(SqlConnection source)
{
    // table -> (lower-case column name -> actual column name)
    var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    await using var cmd = new SqlCommand(
        "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo'", source);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var table = reader.GetString(0);
        if (!result.TryGetValue(table, out var columns))
        {
            result[table] = columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        columns[reader.GetString(1)] = reader.GetString(1);
    }
    return result;
}

static async Task<List<KeyValuePair<object, object>>> ReadSourcePairsAsync(SqlConnection source, string table, string idColumn, string keyColumn)
{
    var pairs = new List<KeyValuePair<object, object>>();
    await using var cmd = new SqlCommand($"SELECT {Sql(idColumn)}, {Sql(keyColumn)} FROM [dbo].{Sql(table)}", source) { CommandTimeout = 0 };
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync()) pairs.Add(new(reader.GetValue(0), reader.GetValue(1)));
    return pairs;
}

static async Task<List<KeyValuePair<object, object>>> ReadTargetPairsAsync(NpgsqlConnection target, NpgsqlTransaction tx, string table, string idColumn, string keyColumn)
{
    var pairs = new List<KeyValuePair<object, object>>();
    await using var cmd = new NpgsqlCommand($"SELECT {Pg(idColumn)}, {Pg(keyColumn)} FROM {Pg(table)}", target, tx);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync()) pairs.Add(new(reader.GetValue(0), reader.GetValue(1)));
    return pairs;
}

// Streams one table from SQL Server into Postgres with binary COPY. Returns the number of rows written.
static async Task<long> CopyRowsAsync(
    SqlConnection source,
    NpgsqlConnection target,
    TargetSchema schema,
    Dictionary<string, Dictionary<string, string>> sourceColumns,
    string table,
    List<string> problems,
    Dictionary<string, Dictionary<object, object>> idMaps,
    HashSet<object>? onlyIds = null)
{
    if (!sourceColumns.TryGetValue(table, out var sourceCols))
    {
        Console.WriteLine($"  {table}: not in the source, skipped");
        return 0;
    }

    var columns = new List<TargetColumn>();
    foreach (var column in schema.Columns[table])
    {
        if (sourceCols.ContainsKey(column.Name))
        {
            columns.Add(column);
        }
        else if (column.Nullable || column.HasDefault)
        {
            Console.WriteLine($"  {table}.{column.Name}: not in the source, left {(column.HasDefault ? "to its default" : "null")}");
        }
        else
        {
            problems.Add($"{table}.{column.Name} is required in the target but missing from the source");
            return 0;
        }
    }
    foreach (var extra in sourceCols.Keys.Where(c => !schema.Columns[table].Any(t => t.Name.Equals(c, StringComparison.OrdinalIgnoreCase))))
    {
        Console.WriteLine($"  {table}.{extra}: only in the source, not copied");
    }

    // Columns pointing at a remapped reference table get their values translated.
    var remaps = columns
        .Select(c => schema.ForeignKeys.FirstOrDefault(fk => fk.Child == table && fk.ChildColumn == c.Name && idMaps.ContainsKey(fk.Parent)))
        .Select(fk => fk is null ? null : idMaps[fk.Parent])
        .ToArray();

    var select = $"SELECT {string.Join(", ", columns.Select(c => Sql(sourceCols[c.Name])))} FROM [dbo].{Sql(table)}";
    var idIndex = columns.FindIndex(c => c.Name == "Id");

    long rows = 0;
    try
    {
        await using var cmd = new SqlCommand(select, source) { CommandTimeout = 0 };
        await using var reader = await cmd.ExecuteReaderAsync();
        await using var writer = await target.BeginBinaryImportAsync(
            $"COPY {Pg(table)} ({string.Join(", ", columns.Select(c => Pg(c.Name)))}) FROM STDIN (FORMAT BINARY)");

        while (await reader.ReadAsync())
        {
            if (onlyIds is not null && (idIndex < 0 || !onlyIds.Contains(reader.GetValue(idIndex)))) continue;

            await writer.StartRowAsync();
            for (var i = 0; i < columns.Count; i++)
            {
                var value = reader.GetValue(i);
                if (value is DBNull)
                {
                    await writer.WriteNullAsync();
                    continue;
                }
                if (remaps[i] is { } map && map.TryGetValue(value, out var mapped)) value = mapped;
                await WriteValueAsync(writer, value, columns[i]);
            }
            rows++;
        }
        await writer.CompleteAsync();
    }
    catch (Exception ex) when (ex is PostgresException or NpgsqlException or SqlException or InvalidCastException or FormatException or OverflowException)
    {
        problems.Add($"{table}: {ex.Message}");
        throw new CopyAbortedException(problems);
    }

    Console.WriteLine($"  {table}: {rows}");
    return rows;
}

static Task WriteValueAsync(NpgsqlBinaryImporter writer, object value, TargetColumn column) => column.Type switch
{
    // SQL Server datetime2 has no offset. The app always wrote UTC, so mark it as such.
    "timestamptz" => writer.WriteAsync(value switch
    {
        DateTimeOffset dto => dto.UtcDateTime,
        DateTime { Kind: DateTimeKind.Local } dt => dt.ToUniversalTime(),
        DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
        _ => DateTime.SpecifyKind(Convert.ToDateTime(value), DateTimeKind.Utc),
    }, NpgsqlDbType.TimestampTz),
    "timestamp" => writer.WriteAsync(DateTime.SpecifyKind(Convert.ToDateTime(value), DateTimeKind.Unspecified), NpgsqlDbType.Timestamp),
    "date" => writer.WriteAsync(value is DateOnly d ? d : DateOnly.FromDateTime(Convert.ToDateTime(value)), NpgsqlDbType.Date),
    "uuid" => writer.WriteAsync(value is Guid g ? g : Guid.Parse(value.ToString()!), NpgsqlDbType.Uuid),
    "int2" => writer.WriteAsync(Convert.ToInt16(value), NpgsqlDbType.Smallint),
    "int4" => writer.WriteAsync(Convert.ToInt32(value), NpgsqlDbType.Integer),
    "int8" => writer.WriteAsync(Convert.ToInt64(value), NpgsqlDbType.Bigint),
    "numeric" => writer.WriteAsync(Convert.ToDecimal(value), NpgsqlDbType.Numeric),
    "float4" => writer.WriteAsync(Convert.ToSingle(value), NpgsqlDbType.Real),
    "float8" => writer.WriteAsync(Convert.ToDouble(value), NpgsqlDbType.Double),
    "bool" => writer.WriteAsync(Convert.ToBoolean(value), NpgsqlDbType.Boolean),
    "citext" => writer.WriteAsync(value.ToString()!, NpgsqlDbType.Citext),
    "varchar" => writer.WriteAsync(value.ToString()!, NpgsqlDbType.Varchar),
    "bpchar" => writer.WriteAsync(value.ToString()!, NpgsqlDbType.Char),
    "text" => writer.WriteAsync(value.ToString()!, NpgsqlDbType.Text),
    "bytea" => writer.WriteAsync((byte[])value, NpgsqlDbType.Bytea),
    _ => throw new InvalidCastException($"{column.Name}: no conversion for Postgres type '{column.Type}'"),
};

// Explicit Ids were inserted into identity columns, so move each sequence past the highest one.
static async Task ResetSequencesAsync(NpgsqlConnection target, NpgsqlTransaction tx, TargetSchema schema, IEnumerable<string> tables)
{
    foreach (var table in tables.Distinct())
    {
        foreach (var column in schema.Columns[table].Where(c => c.IsSequence))
        {
            var sql = $"SELECT setval(pg_get_serial_sequence(@table, @column), COALESCE((SELECT MAX({Pg(column.Name)}) FROM {Pg(table)}), 0) + 1, false)";
            await using var cmd = new NpgsqlCommand(sql, target, tx);
            cmd.Parameters.AddWithValue("table", Pg(table));
            cmd.Parameters.AddWithValue("column", column.Name);
            await cmd.ExecuteScalarAsync();
        }
    }
}

static class Plan
{
    // Seeded with new GUIDs in every database: match source rows to target rows by this column.
    public static readonly Dictionary<string, string> RemapByKey = new()
    {
        ["Achievements"] = "Key",
        ["CommunityChannels"] = "Slug",
        ["SystemTags"] = "Key",
    };

    // Created by admins rather than seeded: copy any rows the target doesn't have, keeping their Ids.
    public static readonly string[] CopyMissing = ["Quizzes", "QuizQuestions"];

    // Users, every table with a UserId column (several have no foreign key), and everything that
    // references those tables. ApplicationSettings comes in through its required UpdatedBy -> Users key.
    public static HashSet<string> UserTables(TargetSchema schema)
    {
        var tables = schema.Tables
            .Where(t => t == "Users" || schema.Columns[t].Any(c => c.Name == "UserId"))
            .ToHashSet();

        bool added;
        do
        {
            added = false;
            foreach (var fk in schema.ForeignKeys)
            {
                if (tables.Contains(fk.Parent) && tables.Add(fk.Child)) added = true;
            }
        } while (added);

        return tables;
    }
}

record TargetColumn(string Name, string Type, bool Nullable, bool HasDefault, bool IsSequence);

record ForeignKey(string Child, string ChildColumn, string Parent);

class TargetSchema
{
    public required HashSet<string> Tables { get; init; }
    public required Dictionary<string, List<TargetColumn>> Columns { get; init; }
    public required List<ForeignKey> ForeignKeys { get; init; }

    public static async Task<TargetSchema> LoadAsync(NpgsqlConnection connection)
    {
        var tables = new HashSet<string>();
        await using (var cmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' AND table_name <> '__EFMigrationsHistory'",
            connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        }

        var columns = tables.ToDictionary(t => t, _ => new List<TargetColumn>());
        await using (var cmd = new NpgsqlCommand(
            """
            SELECT table_name, column_name, udt_name, is_nullable = 'YES', column_default IS NOT NULL OR is_identity = 'YES',
                   is_identity = 'YES' OR COALESCE(column_default LIKE 'nextval(%', false)
            FROM information_schema.columns
            WHERE table_schema = 'public'
            ORDER BY table_name, ordinal_position
            """, connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                if (columns.TryGetValue(reader.GetString(0), out var list))
                {
                    list.Add(new TargetColumn(reader.GetString(1), reader.GetString(2), reader.GetBoolean(3), reader.GetBoolean(4), reader.GetBoolean(5)));
                }
            }
        }

        var foreignKeys = new List<ForeignKey>();
        await using (var cmd = new NpgsqlCommand(
            """
            SELECT child.relname, a.attname, parent.relname
            FROM pg_constraint c
            JOIN pg_class child ON child.oid = c.conrelid
            JOIN pg_class parent ON parent.oid = c.confrelid
            JOIN LATERAL unnest(c.conkey) AS k(attnum) ON true
            JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = k.attnum
            WHERE c.contype = 'f' AND c.connamespace = 'public'::regnamespace
            """, connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) foreignKeys.Add(new ForeignKey(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return new TargetSchema { Tables = tables, Columns = columns, ForeignKeys = foreignKeys };
    }

    // Parents before children. Self-references are fine: COPY checks them at the end of the statement.
    public List<string> TopologicalOrder(HashSet<string> subset)
    {
        var order = new List<string>();
        var state = new Dictionary<string, int>(); // 1 = visiting, 2 = done

        void Visit(string table)
        {
            if (state.GetValueOrDefault(table) == 2) return;
            if (state.GetValueOrDefault(table) == 1) throw new InvalidOperationException($"Foreign key cycle through {table}");
            state[table] = 1;
            foreach (var parent in ForeignKeys.Where(fk => fk.Child == table && fk.Parent != table && subset.Contains(fk.Parent)).Select(fk => fk.Parent).Distinct())
            {
                Visit(parent);
            }
            state[table] = 2;
            order.Add(table);
        }

        foreach (var table in subset.Order()) Visit(table);
        return order;
    }
}

class CopyAbortedException(List<string> problems) : Exception(string.Join(Environment.NewLine, problems));
