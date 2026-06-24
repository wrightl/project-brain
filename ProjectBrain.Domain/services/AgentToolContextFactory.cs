namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

public interface IAgentToolContextFactory
{
    AgentToolContext Create(
        string userId,
        Guid? conversationId,
        Guid? workflowId,
        string? lastUserQuery = null,
        UserType userType = UserType.User);
}

public sealed class AgentToolContextFactory : IAgentToolContextFactory
{
    private readonly IGoalService _goalService;
    private readonly IGoalMutationSideEffects _goalMutationSideEffects;
    private readonly IGoalSuggestionService _goalSuggestionService;
    private readonly ICopingStrategyService _copingStrategyService;
    private readonly ICopingStrategySideEffects _copingStrategySideEffects;
    private readonly IUserKnowledgeUploadService _knowledgeUploadService;
    private readonly ICoachProfileService _coachProfileService;
    private readonly IUserProfileService _userProfileService;
    private readonly IUserService _userService;
    private readonly IConnectionService _connectionService;
    private readonly IStrategySuggestionService _strategySuggestionService;
    private readonly IJournalAgentService _journalAgentService;
    private readonly IJournalEntryService _journalEntryService;
    private readonly IJournalStreakService _journalStreakService;
    private readonly IUserMemoryService _userMemoryService;
    private readonly IAgentMemoryWriteService _agentMemoryWriteService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IFeatureGateService _featureGateService;

    public AgentToolContextFactory(
        IGoalService goalService,
        IGoalMutationSideEffects goalMutationSideEffects,
        IGoalSuggestionService goalSuggestionService,
        ICopingStrategyService copingStrategyService,
        ICopingStrategySideEffects copingStrategySideEffects,
        IUserKnowledgeUploadService knowledgeUploadService,
        ICoachProfileService coachProfileService,
        IUserProfileService userProfileService,
        IUserService userService,
        IConnectionService connectionService,
        IStrategySuggestionService strategySuggestionService,
        IJournalAgentService journalAgentService,
        IJournalEntryService journalEntryService,
        IJournalStreakService journalStreakService,
        IUserMemoryService userMemoryService,
        IAgentMemoryWriteService agentMemoryWriteService,
        IFeatureFlagService featureFlagService,
        IFeatureGateService featureGateService)
    {
        _goalService = goalService;
        _goalMutationSideEffects = goalMutationSideEffects;
        _goalSuggestionService = goalSuggestionService;
        _copingStrategyService = copingStrategyService;
        _copingStrategySideEffects = copingStrategySideEffects;
        _knowledgeUploadService = knowledgeUploadService;
        _coachProfileService = coachProfileService;
        _userProfileService = userProfileService;
        _userService = userService;
        _connectionService = connectionService;
        _strategySuggestionService = strategySuggestionService;
        _journalAgentService = journalAgentService;
        _journalEntryService = journalEntryService;
        _journalStreakService = journalStreakService;
        _userMemoryService = userMemoryService;
        _agentMemoryWriteService = agentMemoryWriteService;
        _featureFlagService = featureFlagService;
        _featureGateService = featureGateService;
    }

    public AgentToolContext Create(
        string userId,
        Guid? conversationId,
        Guid? workflowId,
        string? lastUserQuery = null,
        UserType userType = UserType.User)
    {
        return new AgentToolContext
        {
            UserId = userId,
            UserType = userType,
            LastUserQuery = lastUserQuery,
            ConversationId = conversationId,
            WorkflowId = workflowId,
            GoalService = _goalService,
            GoalMutationSideEffects = _goalMutationSideEffects,
            GoalSuggestionService = _goalSuggestionService,
            CopingStrategyService = _copingStrategyService,
            CopingStrategySideEffects = _copingStrategySideEffects,
            KnowledgeUploadService = _knowledgeUploadService,
            CoachProfileService = _coachProfileService,
            UserProfileService = _userProfileService,
            UserService = _userService,
            ConnectionService = _connectionService,
            StrategySuggestionService = _strategySuggestionService,
            JournalAgentService = _journalAgentService,
            JournalEntryService = _journalEntryService,
            JournalStreakService = _journalStreakService,
            UserMemoryService = _userMemoryService,
            AgentMemoryWriteService = _agentMemoryWriteService,
            FeatureFlagService = _featureFlagService,
            FeatureGateService = _featureGateService
        };
    }
}
