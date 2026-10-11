using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ProjectBrain.Database;

/// <summary>
/// Npgsql only writes <see cref="DateTimeKind.Utc"/> values to <c>timestamp with time zone</c>.
/// Unspecified values (e.g. from client JSON without an offset, or <c>new DateTime(y, m, d)</c>) are treated as UTC,
/// local values are converted, and values read back are marked UTC.
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
