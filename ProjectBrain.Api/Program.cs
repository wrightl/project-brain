using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ProjectBrain.AI;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Middlewares;
using ProjectBrain.Api.Validators;
using Scalar.AspNetCore;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration["AI:UseNewSearchService"]?.ToLower() == "true")
{
    // Register AISeeding for background index seeding
    builder.Services.AddSingleton<AISeeding>();
    builder.Services.AddHostedService<AISeedingBackgroundTask>();
}

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add Redis distributed cache
builder.AddRedisDistributedCache("cache");

// Register keyed HttpClient for EmailService
builder.Services.AddHttpClient("Mailgun", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var apiKey = configuration["Mailgun:ApiKey"]
        ?? throw new InvalidOperationException("Mailgun:ApiKey is not configured");
    var domain = configuration["Mailgun:Domain"]
        ?? throw new InvalidOperationException("Mailgun:Domain is not configured");

    client.BaseAddress = new Uri($"https://api.eu.mailgun.net/v3/{domain}/");

    // Set up Basic Authentication
    var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
}).AddAsKeyed();

builder.AddProjectBrainDomain();

// Add database migrations healthcheck to ensure migrations are applied before marking as healthy
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseMigrationsHealthCheck>("database-migrations");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            // Get allowed origins from configuration, fallback to common development origins
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "https://localhost:6099", "http://localhost:3000", "http://localhost:6099" };

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required when using credentials (Authorization header)
        });
});


builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 300, // TODO: Review this limit later
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.AddCustomAuthentication();
builder.AddCustomAuthorisation();

// Add SignalR
builder.Services.AddSignalR();

builder.AddAzureOpenAI();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIdentityService, IdentityService>();

builder.AddAuth0ManagementApi();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateQuizRequestDtoValidator>();

// Configure JSON serialization to handle circular references
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Add services to the container.
builder.Services.AddProblemDetails();

// // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi(options =>
// {
//     options.AddDocumentTransformer((document, context, cancellationToken) =>
//     {
//         document.Info.Contact = new OpenApiContact
//         {
//             Name = "ProjectBrain Support",
//             Email = "support@dotanddashconsulting.com"
//         };
//         return Task.CompletedTask;
//     });

//     options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
// });

// builder.Services.AddTickerQ(options =>
// {
//     options.SetMaxConcurrency(10);
//     // options.AddOperationalStore<MyDbContext>(efOpt => 
//     // {
//     //     efOpt.SetExceptionHandler<MyExceptionHandlerClass>();
//     //     efOpt.UseModelCustomizerForMigrations();
//     // });
//     if (builder.Environment.IsDevelopment())
//     {
//         options.AddDashboard(uiopt =>
//         {
//             uiopt.BasePath = "/tickerq-dashboard";
//             uiopt.EnableBasicAuth = true;
//         });
//     }
// });

builder.AddFeatureFlags();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// TODO - restore this?
// if (app.Environment.IsDevelopment())
// {
// app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Servers = [];
});
// }

// app.UseSecureHeaders();

// TODO: Decide if this should befixed for .net10 and restored
// app.UseRobotMiddleware();

app.UseCors();

app.UseRateLimiter();

app.UseCustomAuthentication();
app.UseUserActivityTracking(); // Track user activity after authentication
app.UseCustomAuthorisation();

// Add api's
app.MapUserEndpoints();
app.MapUserManagementEndpoints();
app.MapChatEndpoints();
app.MapConversationEndpoints();
app.MapResourceEndpoints();
app.MapCoachEndpoints();
app.MapConnectionEndpoints();
app.MapVoiceNoteEndpoints();
app.MapCoachMessageEndpoints();
app.MapQuizEndpoints();
app.MapStatisticsEndpoints();
app.MapSubscriptionEndpoints();
app.MapStripeWebhookEndpoints();
app.MapAuth0WebhookEndpoints();
app.MapSubscriptionManagementEndpoints();
app.MapSubscriptionAnalyticsEndpoints();
app.MapJournalEndpoints();
app.MapTagEndpoints();
app.MapGoalEndpoints();
app.MapFeatureFlagEndpoints();

// Map SignalR hub
app.MapHub<ProjectBrain.Api.Hubs.CoachMessageHub>("/hubs/coach-messages").RequireAuthorization();

app.MapDefaultEndpoints();

// app.UseTickerQ(); // Activates job processor

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }