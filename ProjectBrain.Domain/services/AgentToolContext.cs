namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

public sealed class AgentToolContext
{
    public required string UserId { get; init; }
    public string? LastUserQuery { get; init; }
    public Guid? ConversationId { get; init; }
    public Guid? WorkflowId { get; init; }
    public required IGoalService GoalService { get; init; }
    public required IGoalMutationSideEffects GoalMutationSideEffects { get; init; }
    public ICopingStrategyService? CopingStrategyService { get; init; }
    public ICopingStrategySideEffects? CopingStrategySideEffects { get; init; }
    public IUserKnowledgeUploadService? KnowledgeUploadService { get; init; }
    public ICoachProfileService? CoachProfileService { get; init; }
    public IUserProfileService? UserProfileService { get; init; }
    public IUserService? UserService { get; init; }
    public IConnectionService? ConnectionService { get; init; }
    public IStrategySuggestionService? StrategySuggestionService { get; init; }
}
