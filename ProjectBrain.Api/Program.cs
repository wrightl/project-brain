using Microsoft.AspNetCore.Authentication.JwtBearer;
using ProjectBrain.AI;
using ProjectBrain.Api.Authentication;
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

builder.AddProjectBrainDomain();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});
builder.AddCustomAuthentication();
builder.AddCustomAuthorisation();

builder.AddAzureOpenAI();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIdentityService, IdentityService>();

builder.AddAuth0ManagementApi();

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

builder.Services.AddFeatureFlags();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

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

// app.UseCors();
app.UseCustomAuthentication();
app.UseCustomAuthorisation();

// Add api's
app.MapUserEndpoints();
app.MapChatEndpoints();
app.MapConversationEndpoints();
app.MapResourceEndpoints();

app.MapDefaultEndpoints();

// app.UseTickerQ(); // Activates job processor

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }