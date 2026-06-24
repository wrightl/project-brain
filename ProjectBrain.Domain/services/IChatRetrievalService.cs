namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

public interface IChatRetrievalService
{
    Task<ChatRetrievalResult> RetrieveAsync(
        string userQuery,
        string userId,
        ChatMemoryContext memoryContext,
        string? traceId = null,
        CancellationToken cancellationToken = default);
}
