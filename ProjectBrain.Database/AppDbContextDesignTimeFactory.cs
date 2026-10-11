using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace ProjectBrain.Database;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the model without starting the Aspire host.
/// The connection string is only used by commands that touch a database (e.g. <c>database update</c>);
/// override it with the <c>PROJECTBRAIN_DESIGN_CONNECTION</c> environment variable.
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PROJECTBRAIN_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=49976;Database=projectbrain;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options, NullLogger<AppDbContext>.Instance);
    }
}
