using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public static class ProgramExtensions
{
    public static void AddProjectBrainDomain(this WebApplicationBuilder builder)
    {
        builder.AddProjectBrainDbContext();

        // Register Unit of Work
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Repositories
        builder.Services.AddScoped<IVoiceNoteRepository, VoiceNoteRepository>();
        builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
        builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
        builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
        builder.Services.AddScoped<IQuizRepository, QuizRepository>();
        builder.Services.AddScoped<IQuizResponseRepository, QuizResponseRepository>();
        builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        builder.Services.AddScoped<ICoachProfileRepository, CoachProfileRepository>();
        builder.Services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
        builder.Services.AddScoped<ITagRepository, TagRepository>();
        builder.Services.AddScoped<ICoachRatingRepository, CoachRatingRepository>();
        builder.Services.AddScoped<IGoalRepository, GoalRepository>();
        builder.Services.AddScoped<IOnboardingDataRepository, OnboardingDataRepository>();
        builder.Services.AddScoped<IAgentWorkflowRepository, AgentWorkflowRepository>();
        builder.Services.AddScoped<IAgentActionRepository, AgentActionRepository>();
        builder.Services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();

        // Register Cache Service
        builder.Services.AddScoped<ProjectBrain.Domain.Caching.ICacheService, ProjectBrain.Domain.Caching.RedisCacheService>();

        // Register Services
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
        builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
        builder.Services.AddScoped<IUsageTrackingService, UsageTrackingService>();
        builder.Services.AddScoped<IFeatureGateService, FeatureGateService>();
        builder.Services.AddScoped<IStripeService, StripeService>();
        builder.Services.AddScoped<ISubscriptionAnalyticsService, SubscriptionAnalyticsService>();
        builder.Services.AddScoped<ICoachMessageService, CoachMessageService>();
        builder.Services.AddScoped<IJournalEntryService, JournalEntryService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<ICoachRatingService, CoachRatingService>();
        builder.Services.AddScoped<IGoalService, GoalService>();
        builder.Services.AddScoped<IOnboardingDataService, OnboardingDataService>();
        builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        builder.Services.AddScoped<IAgentService, AgentService>();
        builder.Services.AddScoped<IAgentActionTrackingService, AgentActionTrackingService>();

        // Register mail provider
        builder.Services.AddScoped<IEmailService, MailgunEmailService>();

        // Register push notification service
        builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

        // Register device token cleanup service
        builder.Services.AddScoped<IDeviceTokenCleanupService, DeviceTokenCleanupService>();

        // Register background services
        builder.Services.AddHostedService<UserActivitySyncService>();
        builder.Services.AddHostedService<DeviceTokenCleanupBackgroundService>();
    }
}