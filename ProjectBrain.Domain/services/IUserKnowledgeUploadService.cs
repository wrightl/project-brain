namespace ProjectBrain.Domain;

public interface IUserKnowledgeUploadService
{
    Task<KnowledgeUploadResult> UploadMarkdownAsync(
        string userId,
        string filename,
        string markdown,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeResourceSummary>> ListResourcesAsync(
        string userId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteResourceAsync(
        string userId,
        Guid resourceId,
        CancellationToken cancellationToken = default);
}

public sealed class KnowledgeUploadResult
{
    public required bool Success { get; init; }
    public Guid? ResourceId { get; init; }
    public string? Filename { get; init; }
    public string? Message { get; init; }
}

public sealed class KnowledgeResourceSummary
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public int SizeInBytes { get; init; }
    public DateTime CreatedAt { get; init; }
}
