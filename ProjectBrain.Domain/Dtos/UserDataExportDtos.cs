namespace ProjectBrain.Domain.Dtos;

public sealed class UserDataExport
{
    public required string UserId { get; init; }
    public DateTime ExportedAt { get; init; } = DateTime.UtcNow;
    public UserDataExportProfile? Profile { get; init; }
    public UserMemoryListDto? Memories { get; init; }
    public IReadOnlyList<UserDataExportConversation> Conversations { get; init; } = Array.Empty<UserDataExportConversation>();
    public IReadOnlyList<UserDataExportJournalEntry> JournalEntries { get; init; } = Array.Empty<UserDataExportJournalEntry>();
    public IReadOnlyList<UserDataExportGoal> Goals { get; init; } = Array.Empty<UserDataExportGoal>();
    public IReadOnlyList<UserDataExportStrategy> Strategies { get; init; } = Array.Empty<UserDataExportStrategy>();
    public IReadOnlyList<UserDataExportQuizResponse> QuizResponses { get; init; } = Array.Empty<UserDataExportQuizResponse>();
    public UserDataExportSubscription? Subscription { get; init; }
}

public sealed class UserDataExportProfile
{
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string? PreferredPronoun { get; init; }
    public IReadOnlyList<string> NeurodiverseTraits { get; init; } = Array.Empty<string>();
    public string? PreferencesJson { get; init; }
}

public sealed class UserDataExportConversation
{
    public Guid Id { get; init; }
    public string? Title { get; init; }
    public string? ContextSummary { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<UserDataExportChatMessage> Messages { get; init; } = Array.Empty<UserDataExportChatMessage>();
}

public sealed class UserDataExportChatMessage
{
    public int Id { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class UserDataExportJournalEntry
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class UserDataExportGoal
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public int Index { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Completed { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public sealed class UserDataExportStrategy
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int? Rating { get; init; }
    public DateTime SavedAt { get; init; }
}

public sealed class UserDataExportQuizResponse
{
    public Guid Id { get; init; }
    public Guid QuizId { get; init; }
    public string? QuizTitle { get; init; }
    public string AnswersJson { get; init; } = "{}";
    public decimal? Score { get; init; }
    public DateTime CompletedAt { get; init; }
}

public sealed class UserDataExportSubscription
{
    public string? Status { get; init; }
    public string? TierName { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
}
