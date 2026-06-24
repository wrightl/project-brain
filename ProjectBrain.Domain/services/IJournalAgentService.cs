namespace ProjectBrain.Domain;

public interface IJournalAgentService
{
    Task<JournalAgentEntryResult> CreateEntryAsync(
        string userId,
        string content,
        CancellationToken cancellationToken = default);
}

public sealed class JournalAgentEntryResult
{
    public required Guid Id { get; init; }
    public string? Summary { get; init; }
    public required DateTime CreatedAt { get; init; }
}
