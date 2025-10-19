using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectBrain.Api.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Add in-memory configuration for testing
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Provide dummy connection string to satisfy Aspire validation
                ["ConnectionStrings:projectbraindb"] = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;",
                ["Aspire:Microsoft:EntityFrameworkCore:SqlServer:DisableHealthChecks"] = "true",
                // Disable other Aspire features that might interfere
                ["OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY"] = "none"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the Aspire-registered DbContext
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove DbContextOptions registration
            var optionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optionsDescriptor != null)
            {
                services.Remove(optionsDescriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var logger = sp.GetRequiredService<ILogger<AppDbContext>>();
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid().ToString());
            });

            // Ensure database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}
