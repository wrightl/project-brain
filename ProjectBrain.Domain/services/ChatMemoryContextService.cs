namespace ProjectBrain.Domain;

using System.Text.Json;
using ProjectBrain.Domain.Dtos;

public class ChatMemoryContextService : IChatMemoryContextService
{
    private readonly IUserProfileService _userProfileService;
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly IConversationService _conversationService;

    public ChatMemoryContextService(
        IUserProfileService userProfileService,
        IApplicationSettingsService applicationSettingsService,
        IConversationService conversationService)
    {
        _userProfileService = userProfileService;
        _applicationSettingsService = applicationSettingsService;
        _conversationService = conversationService;
    }

    public async Task<ChatMemoryContext> BuildAsync(
        string userId,
        Guid? conversationId,
        CancellationToken cancellationToken = default)
    {
        var (aiSettings, policies) = await _applicationSettingsService.GetChatMemoryApplicationSettingsAsync(cancellationToken);
        var profile = await _userProfileService.GetByUserId(userId);

        string? conversationSummary = null;
        if (conversationId.HasValue && aiSettings.EnableConversationSummary)
        {
            var conversation = await _conversationService.GetById(conversationId.Value, userId);
            if (!string.IsNullOrWhiteSpace(conversation?.ContextSummary))
            {
                conversationSummary = conversation.ContextSummary;
            }
        }

        UserChatPreferences? userPreferences = null;
        if (profile != null)
        {
            var traits = profile.NeurodiverseTraits?
                .Select(t => t.Trait)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList() ?? new List<string>();

            var preferencesJson = profile.Preference?.Preferences;
            var parsed = ParsePreferencesJson(preferencesJson);

            if (!string.IsNullOrWhiteSpace(profile.PreferredPronoun)
                || traits.Count > 0
                || !string.IsNullOrWhiteSpace(preferencesJson))
            {
                userPreferences = new UserChatPreferences
                {
                    PreferredPronoun = profile.PreferredPronoun,
                    NeurodiverseTraits = traits,
                    PreferencesJson = preferencesJson,
                    ParsedPreferences = parsed
                };
            }
        }

        return new ChatMemoryContext
        {
            Policies = policies,
            UserPreferences = userPreferences,
            ConversationSummary = conversationSummary,
            RecentMessageWindow = aiSettings.RecentMessageWindow,
            MaxHistoryMessages = aiSettings.MaxHistoryMessages,
            EnableConversationSummary = aiSettings.EnableConversationSummary
        };
    }

    private static IReadOnlyDictionary<string, string> ParsePreferencesJson(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    _ => prop.Value.GetRawText()
                };

                if (!string.IsNullOrWhiteSpace(value))
                {
                    result[prop.Name] = value;
                }
            }

            return result;
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

public interface IChatMemoryContextService
{
    Task<ChatMemoryContext> BuildAsync(string userId, Guid? conversationId, CancellationToken cancellationToken = default);
}
