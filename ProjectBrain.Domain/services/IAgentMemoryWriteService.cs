namespace ProjectBrain.Domain;

public interface IAgentMemoryWriteService
{
    Task<AgentRememberFactResult> RememberFactAsync(
        string userId,
        string content,
        string? category,
        Guid? conversationId,
        CancellationToken cancellationToken = default);
}

public sealed class AgentRememberFactResult
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public string? Category { get; init; }
}
