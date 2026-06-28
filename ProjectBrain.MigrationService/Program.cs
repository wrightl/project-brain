using ProjectBrain.Auth;
using ProjectBrain.Database;
using ProjectBrain.Database.Interfaces;
using ProjectBrain.MigrationService;
using ProjectBrain.MigrationService.Seeding;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.AddAuth();
builder.Services.AddScoped<IIdentitySeedingService, IdentitySeedingService>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(
        Worker.ActivitySourceName,
        DatabaseSeeder.ActivitySourceName));

builder.AddSqlServerDbContext<AppDbContext>(connectionName: "projectbraindb");

var host = builder.Build();
host.Run();
