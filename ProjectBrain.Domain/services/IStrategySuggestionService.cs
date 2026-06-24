namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

public interface IStrategySuggestionService
{
    Task<List<StrategySuggestion>> GetSuggestionsAsync(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<AgentChatMessage> history,
        ChatMemoryContext memoryContext,
        Guid? conversationId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}

public sealed class StrategySuggestion
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? IconKey { get; init; }
}
