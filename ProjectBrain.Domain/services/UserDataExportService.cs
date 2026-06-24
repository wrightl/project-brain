namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;

public class UserDataExportService : IUserDataExportService
{
    private readonly IUserService _userService;
    private readonly IUserProfileService _userProfileService;
    private readonly IUserFactService _userFactService;
    private readonly IUserEpisodeService _userEpisodeService;
    private readonly IConversationService _conversationService;
    private readonly IJournalEntryService _journalEntryService;
    private readonly IGoalRepository _goalRepository;
    private readonly ICopingStrategyService _copingStrategyService;
    private readonly IQuizResponseRepository _quizResponseRepository;
    private readonly ISubscriptionService _subscriptionService;

    public UserDataExportService(
        IUserService userService,
        IUserProfileService userProfileService,
        IUserFactService userFactService,
        IUserEpisodeService userEpisodeService,
        IConversationService conversationService,
        IJournalEntryService journalEntryService,
        IGoalRepository goalRepository,
        ICopingStrategyService copingStrategyService,
        IQuizResponseRepository quizResponseRepository,
        ISubscriptionService subscriptionService)
    {
        _userService = userService;
        _userProfileService = userProfileService;
        _userFactService = userFactService;
        _userEpisodeService = userEpisodeService;
        _conversationService = conversationService;
        _journalEntryService = journalEntryService;
        _goalRepository = goalRepository;
        _copingStrategyService = copingStrategyService;
        _quizResponseRepository = quizResponseRepository;
        _subscriptionService = subscriptionService;
    }

    public async Task<UserDataExport> ExportUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetById(userId);
        var profile = await _userProfileService.GetByUserId(userId);

        var factList = await _userFactService.ListForUserAsync(userId, includeProvisional: true, cancellationToken);
        var episodes = await _userEpisodeService.ListForUserAsync(userId, includeProvisional: true, cancellationToken);
        var memories = new UserMemoryListDto
        {
            Facts = factList.Facts,
            Episodes = episodes
        };

        var conversations = new List<UserDataExportConversation>();
        foreach (var conversation in await _conversationService.GetAllForUser(userId))
        {
            var withMessages = await _conversationService.GetByIdWithMessages(conversation.Id, userId);
            conversations.Add(new UserDataExportConversation
            {
                Id = conversation.Id,
                Title = conversation.Title,
                ContextSummary = conversation.ContextSummary,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                Messages = (withMessages?.Messages ?? [])
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new UserDataExportChatMessage
                    {
                        Id = m.Id,
                        Role = m.Role,
                        Content = m.Content,
                        CreatedAt = m.CreatedAt
                    })
                    .ToList()
            });
        }

        var journalEntries = (await _journalEntryService.GetAllForUser(userId))
            .Select(j => new UserDataExportJournalEntry
            {
                Id = j.Id,
                Content = j.Content,
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt
            })
            .ToList();

        var goals = (await _goalRepository.FindAsync(g => g.UserId == userId, cancellationToken))
            .Select(g => new UserDataExportGoal
            {
                Id = g.Id,
                Date = g.Date,
                Index = g.Index,
                Message = g.Message,
                Completed = g.Completed,
                CompletedAt = g.CompletedAt
            })
            .ToList();

        var strategies = (await _copingStrategyService.GetLibraryAsync(userId, cancellationToken))
            .Select(s => new UserDataExportStrategy
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Rating = s.Rating,
                SavedAt = s.SavedAt
            })
            .ToList();

        var quizResponses = (await _quizResponseRepository.GetAllForUserAsync(userId, cancellationToken))
            .Select(q => new UserDataExportQuizResponse
            {
                Id = q.Id,
                QuizId = q.QuizId,
                QuizTitle = q.Quiz?.Title,
                AnswersJson = q.AnswersJson,
                Score = q.Score,
                CompletedAt = q.CompletedAt
            })
            .ToList();

        UserDataExportSubscription? subscriptionExport = null;
        var subscription = await _subscriptionService.GetUserSubscriptionAsync(userId, UserType.User)
            ?? await _subscriptionService.GetUserSubscriptionAsync(userId, UserType.Coach);
        if (subscription is not null)
        {
            var tierName = subscription.Tier?.Name
                ?? await _subscriptionService.GetUserTierAsync(userId, UserType.User);
            subscriptionExport = new UserDataExportSubscription
            {
                Status = subscription.Status,
                TierName = tierName,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd
            };
        }

        return new UserDataExport
        {
            UserId = userId,
            Profile = new UserDataExportProfile
            {
                Email = user?.Email,
                FullName = user?.FullName,
                PreferredPronoun = profile?.PreferredPronoun,
                NeurodiverseTraits = profile?.NeurodiverseTraits?.Select(t => t.Trait).ToList() ?? [],
                PreferencesJson = profile?.Preference?.Preferences
            },
            Memories = memories,
            Conversations = conversations,
            JournalEntries = journalEntries,
            Goals = goals,
            Strategies = strategies,
            QuizResponses = quizResponses,
            Subscription = subscriptionExport
        };
    }
}

public interface IUserDataExportService
{
    Task<UserDataExport> ExportUserDataAsync(string userId, CancellationToken cancellationToken = default);
}
