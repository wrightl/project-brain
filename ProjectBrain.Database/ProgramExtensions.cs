using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class ProgramExtensions
{
    public static void AddProjectBrainDbContext(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<ProjectBrainDbInitializer>();
        // builder.Services.AddOpenTelemetry()
        //     .WithTracing(tracing => tracing.AddSource(ProjectBrainDbInitializer.ActivitySourceName));

        // sql
        builder.AddSqlServerDbContext<AppDbContext>(connectionName: "projectbraindb");

        // builder.Services.AddScoped<IMovieService, MovieService>();
        // builder.Services.AddScoped<IEggService, EggService>();
        // builder.Services.AddScoped<IUserService, UserService>();
        // builder.Services.AddScoped<IChatService, ChatService>();
        // builder.Services.AddScoped<IConversationService, ConversationService>();
    }
}