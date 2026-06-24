namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

public sealed class AgentToolContext
{
    public required string UserId { get; init; }
    public UserType UserType { get; init; } = UserType.User;
    public string? LastUserQuery { get; init; }
    public Guid? ConversationId { get; init; }
    public Guid? WorkflowId { get; init; }
    public required IGoalService GoalService { get; init; }
    public required IGoalMutationSideEffects GoalMutationSideEffects { get; init; }
    public IGoalSuggestionService? GoalSuggestionService { get; init; }
    public ICopingStrategyService? CopingStrategyService { get; init; }
    public ICopingStrategySideEffects? CopingStrategySideEffects { get; init; }
    public IUserKnowledgeUploadService? KnowledgeUploadService { get; init; }
    public ICoachProfileService? CoachProfileService { get; init; }
    public IUserProfileService? UserProfileService { get; init; }
    public IUserService? UserService { get; init; }
    public IConnectionService? ConnectionService { get; init; }
    public IStrategySuggestionService? StrategySuggestionService { get; init; }
    public IJournalAgentService? JournalAgentService { get; init; }
    public IJournalEntryService? JournalEntryService { get; init; }
    public IJournalStreakService? JournalStreakService { get; init; }
    public IUserMemoryService? UserMemoryService { get; init; }
    public IAgentMemoryWriteService? AgentMemoryWriteService { get; init; }
    public IFeatureFlagService? FeatureFlagService { get; init; }
    public IFeatureGateService? FeatureGateService { get; init; }
}
