namespace ProjectBrain.Api.Background;

/// <summary>Request for journal entry upload + index job.</summary>
public class JournalUploadRequest
{
    public required string UserId { get; init; }
    public required Guid EntryId { get; init; }
}

/// <summary>Request for journal entry delete from blob + index.</summary>
public class JournalDeleteRequest
{
    public required string UserId { get; init; }
    public required Guid EntryId { get; init; }
}

/// <summary>Request for goals markdown upload + index.</summary>
public class GoalsUploadRequest
{
    public required string UserId { get; init; }
}

/// <summary>Request for coping strategy markdown upload + index.</summary>
public class StrategyUploadRequest
{
    public required string UserId { get; init; }
    public required Guid StrategyId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? IconKey { get; init; }
    public int? Rating { get; init; }
    public DateTime SavedAt { get; init; }
}

/// <summary>Request for voice note transcribe + transcript upload + index.</summary>
public class VoiceNoteTranscribeRequest
{
    public required string UserId { get; init; }
    public required Guid VoiceNoteId { get; init; }
    public required string AudioBlobName { get; init; }
}

/// <summary>Request to generate an AI conversation title and persist it (deferred from chat stream).</summary>
public class ConversationTitleSummaryRequest
{
    public required string UserId { get; init; }
    public required Guid ConversationId { get; init; }
    public required string UserMessageContent { get; init; }
}

/// <summary>Request to update rolling conversation context summary after chat persistence.</summary>
public class ConversationContextSummaryRequest
{
    public required string UserId { get; init; }
    public required Guid ConversationId { get; init; }
}

/// <summary>Request to extract and promote memory candidates after chat persistence.</summary>
public class MemoryExtractionRequest
{
    public required string UserId { get; init; }
    public required Guid ConversationId { get; init; }
    public required string UserContent { get; init; }
    public required string AssistantContent { get; init; }
}

/// <summary>Request to apply memory TTL/decay for one user or all users.</summary>
public class MemoryDecayRequest
{
    public string? UserId { get; init; }
}
