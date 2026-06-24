namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

public interface IAgentToolContextFactory
{
    AgentToolContext Create(string userId, Guid? conversationId, Guid? workflowId, string? lastUserQuery = null);
}

public sealed class AgentToolContextFactory : IAgentToolContextFactory
{
    private readonly IGoalService _goalService;
    private readonly IGoalMutationSideEffects _goalMutationSideEffects;
    private readonly ICopingStrategyService _copingStrategyService;
    private readonly ICopingStrategySideEffects _copingStrategySideEffects;
    private readonly IUserKnowledgeUploadService _knowledgeUploadService;
    private readonly ICoachProfileService _coachProfileService;
    private readonly IUserProfileService _userProfileService;
    private readonly IUserService _userService;
    private readonly IConnectionService _connectionService;
    private readonly IStrategySuggestionService _strategySuggestionService;

    public AgentToolContextFactory(
        IGoalService goalService,
        IGoalMutationSideEffects goalMutationSideEffects,
        ICopingStrategyService copingStrategyService,
        ICopingStrategySideEffects copingStrategySideEffects,
        IUserKnowledgeUploadService knowledgeUploadService,
        ICoachProfileService coachProfileService,
        IUserProfileService userProfileService,
        IUserService userService,
        IConnectionService connectionService,
        IStrategySuggestionService strategySuggestionService)
    {
        _goalService = goalService;
        _goalMutationSideEffects = goalMutationSideEffects;
        _copingStrategyService = copingStrategyService;
        _copingStrategySideEffects = copingStrategySideEffects;
        _knowledgeUploadService = knowledgeUploadService;
        _coachProfileService = coachProfileService;
        _userProfileService = userProfileService;
        _userService = userService;
        _connectionService = connectionService;
        _strategySuggestionService = strategySuggestionService;
    }

    public AgentToolContext Create(string userId, Guid? conversationId, Guid? workflowId, string? lastUserQuery = null)
    {
        return new AgentToolContext
        {
            UserId = userId,
            LastUserQuery = lastUserQuery,
            ConversationId = conversationId,
            WorkflowId = workflowId,
            GoalService = _goalService,
            GoalMutationSideEffects = _goalMutationSideEffects,
            CopingStrategyService = _copingStrategyService,
            CopingStrategySideEffects = _copingStrategySideEffects,
            KnowledgeUploadService = _knowledgeUploadService,
            CoachProfileService = _coachProfileService,
            UserProfileService = _userProfileService,
            UserService = _userService,
            ConnectionService = _connectionService,
            StrategySuggestionService = _strategySuggestionService
        };
    }
}
