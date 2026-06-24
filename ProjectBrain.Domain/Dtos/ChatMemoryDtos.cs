namespace ProjectBrain.Domain.Dtos;

/// <summary>Admin-managed chat policy loaded from ApplicationSettings (category AI:Policy).</summary>
public sealed class ChatPolicyItem
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? Description { get; init; }
}

/// <summary>User-specific preferences injected into chat prompts every turn.</summary>
public sealed class UserChatPreferences
{
    public string? PreferredPronoun { get; init; }
    public IReadOnlyList<string> NeurodiverseTraits { get; init; } = Array.Empty<string>();
    public string? PreferencesJson { get; init; }
    public IReadOnlyDictionary<string, string> ParsedPreferences { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Assembled memory context for a single chat turn (Path A + in-thread summary).</summary>
public sealed class ChatMemoryContext
{
    public IReadOnlyList<ChatPolicyItem> Policies { get; init; } = Array.Empty<ChatPolicyItem>();
    public UserChatPreferences? UserPreferences { get; init; }
    public string? ConversationSummary { get; init; }
    public int RecentMessageWindow { get; init; } = 4;
    public int MaxHistoryMessages { get; init; } = 10;
    public bool EnableConversationSummary { get; init; } = true;
}
