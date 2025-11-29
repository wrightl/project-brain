using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProjectBrain.Domain;

public static class ProgramExtensions
{
    public static void AddProjectBrainDomain(this WebApplicationBuilder builder)
    {
        builder.AddProjectBrainDbContext();

        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IUserManagementService, UserManagementService>();
        builder.Services.AddScoped<IChatService, ChatService>();
        builder.Services.AddScoped<IConversationService, ConversationService>();
        builder.Services.AddScoped<IResourceService, ResourceService>();
        builder.Services.AddScoped<ICoachProfileService, CoachProfileService>();
        builder.Services.AddScoped<IUserProfileService, UserProfileService>();
        builder.Services.AddScoped<IConnectionService, ConnectionService>();
        builder.Services.AddScoped<IVoiceNoteService, VoiceNoteService>();
        builder.Services.AddScoped<IQuizService, QuizService>();
        builder.Services.AddScoped<IQuizResponseService, QuizResponseService>();
        builder.Services.AddScoped<IStatisticsService, StatisticsService>();
        builder.Services.AddScoped<IUserActivityService, UserActivityService>();
        
        // Register background service for syncing activity data
        builder.Services.AddHostedService<UserActivitySyncService>();
    }
}