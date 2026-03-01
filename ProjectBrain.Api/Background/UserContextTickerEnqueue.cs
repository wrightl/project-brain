using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace ProjectBrain.Api.Background;

/// <summary>Enqueue user-context jobs to TickerQ.</summary>
public static class UserContextTickerEnqueue
{
    public const string JournalUpload = "UserContext_JournalUpload";
    public const string JournalDelete = "UserContext_JournalDelete";
    public const string GoalsUpload = "UserContext_GoalsUpload";
    public const string StrategyUpload = "UserContext_StrategyUpload";
    public const string VoiceNoteTranscribe = "UserContext_VoiceNoteTranscribe";

    public static async Task EnqueueJournalUploadAsync(
        ITimeTickerManager<TimeTickerEntity> manager,
        string userId,
        Guid entryId,
        CancellationToken ct = default)
    {
        await manager.AddAsync(new TimeTickerEntity
        {
            Function = JournalUpload,
            ExecutionTime = DateTime.UtcNow,
            Request = TickerHelper.CreateTickerRequest(new JournalUploadRequest { UserId = userId, EntryId = entryId })
        }, ct);
    }

    public static async Task EnqueueJournalDeleteAsync(
        ITimeTickerManager<TimeTickerEntity> manager,
        string userId,
        Guid entryId,
        CancellationToken ct = default)
    {
        await manager.AddAsync(new TimeTickerEntity
        {
            Function = JournalDelete,
            ExecutionTime = DateTime.UtcNow,
            Request = TickerHelper.CreateTickerRequest(new JournalDeleteRequest { UserId = userId, EntryId = entryId })
        }, ct);
    }

    public static async Task EnqueueGoalsUploadAsync(
        ITimeTickerManager<TimeTickerEntity> manager,
        string userId,
        CancellationToken ct = default)
    {
        await manager.AddAsync(new TimeTickerEntity
        {
            Function = GoalsUpload,
            ExecutionTime = DateTime.UtcNow,
            Request = TickerHelper.CreateTickerRequest(new GoalsUploadRequest { UserId = userId })
        }, ct);
    }

    public static async Task EnqueueStrategyUploadAsync(
        ITimeTickerManager<TimeTickerEntity> manager,
        StrategyUploadRequest request,
        CancellationToken ct = default)
    {
        await manager.AddAsync(new TimeTickerEntity
        {
            Function = StrategyUpload,
            ExecutionTime = DateTime.UtcNow,
            Request = TickerHelper.CreateTickerRequest(request)
        }, ct);
    }

    public static async Task EnqueueVoiceNoteTranscribeAsync(
        ITimeTickerManager<TimeTickerEntity> manager,
        string userId,
        Guid voiceNoteId,
        string audioBlobName,
        CancellationToken ct = default)
    {
        await manager.AddAsync(new TimeTickerEntity
        {
            Function = VoiceNoteTranscribe,
            ExecutionTime = DateTime.UtcNow,
            Request = TickerHelper.CreateTickerRequest(new VoiceNoteTranscribeRequest
            {
                UserId = userId,
                VoiceNoteId = voiceNoteId,
                AudioBlobName = audioBlobName
            })
        }, ct);
    }
}
