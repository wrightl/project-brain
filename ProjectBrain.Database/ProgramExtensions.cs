using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectBrain.Database;
using ProjectBrain.Database.Interfaces;
using ProjectBrain.Database.Seeders;

public static class ProgramExtensions
{
    public static void AddProjectBrainDbContext(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsEnvironment("Testing"))
            return;

        builder.Services.AddSingleton<IDatabaseStartupState, DatabaseStartupState>();
        builder.Services.AddHostedService<DatabaseStartupHostedService>();
        builder.Services.AddScoped<IDevelopmentDataSeeder, DevelopmentDataSeeder>();

        // postgres — disable Aspire EF health checks so readiness probes do not wake the database
        builder.AddNpgsqlDbContext<AppDbContext>(
            connectionName: "projectbraindb",
            configureSettings: settings => settings.DisableHealthChecks = true);
    }
}