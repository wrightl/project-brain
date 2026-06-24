namespace ProjectBrain.Shared.Dtos.Settings;

/// <summary>DTO for chat memory (conversation summary) settings.</summary>
public class ChatMemorySettingsDto
{
    public required int RecentMessageWindow { get; init; }
    public required int ConversationSummaryInterval { get; init; }
    public required int MaxConversationSummaryLength { get; init; }
    public required bool EnableConversationSummary { get; init; }
}

/// <summary>Request DTO for updating chat memory settings.</summary>
public class UpdateChatMemorySettingsRequestDto
{
    public required int RecentMessageWindow { get; init; }
    public required int ConversationSummaryInterval { get; init; }
    public required int MaxConversationSummaryLength { get; init; }
    public required bool EnableConversationSummary { get; init; }
}
