namespace ProjectBrain.Domain.Dtos;

public sealed class ErasureResult
{
    public required string UserId { get; init; }
    public bool SubscriptionCanceled { get; set; }
    public int SearchDocumentsDeleted { get; set; }
    public int BlobFilesDeleted { get; set; }
    public int MemoryPromotionAuditsDeleted { get; set; }
    public int QuizResponsesDeleted { get; set; }
    public int TagsDeleted { get; set; }
    public int CoachMessagesDeleted { get; set; }
    public int MemoryIndexEntriesDeleted { get; set; }
    public bool UserRowDeleted { get; set; }
    public List<string> Warnings { get; init; } = new();
}
