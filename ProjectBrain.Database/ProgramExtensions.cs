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
        builder.Services.AddHostedService<ProjectBrainDbInitializer>();
        builder.Services.AddScoped<IDevelopmentDataSeeder, DevelopmentDataSeeder>();
        // builder.Services.AddOpenTelemetry()
        //     .WithTracing(tracing => tracing.AddSource(ProjectBrainDbInitializer.ActivitySourceName));

        // sql
        builder.AddSqlServerDbContext<AppDbContext>(connectionName: "projectbraindb");
        // // Register DbContext base type to resolve to AppDbContext for UnitOfWork
        // builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
    }
}