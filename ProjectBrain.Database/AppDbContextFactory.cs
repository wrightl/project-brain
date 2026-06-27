using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options, NullLogger<AppDbContext>.Instance);
    }

    private static string ResolveConnectionString(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--connection", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return Environment.GetEnvironmentVariable("ConnectionStrings__projectbraindb")
            ?? Environment.GetEnvironmentVariable("EF_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=ProjectBrain;Trusted_Connection=True;";
    }
}
