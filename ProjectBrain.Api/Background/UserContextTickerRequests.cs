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
