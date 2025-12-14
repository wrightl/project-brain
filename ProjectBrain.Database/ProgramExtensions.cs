using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
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

        // Register DbContext base type to resolve to AppDbContext for UnitOfWork
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // builder.Services.AddScoped<IMovieService, MovieService>();
        // builder.Services.AddScoped<IEggService, EggService>();
        // builder.Services.AddScoped<IUserService, UserService>();
        // builder.Services.AddScoped<IChatService, ChatService>();
        // builder.Services.AddScoped<IConversationService, ConversationService>();
    }
}