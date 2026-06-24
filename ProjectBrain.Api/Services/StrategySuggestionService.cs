using ProjectBrain.AI;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;
using _shared = ProjectBrain.Models;

namespace ProjectBrain.Api.Services;

public sealed class StrategySuggestionService : IStrategySuggestionService
{
    private readonly AzureOpenAI _azureOpenAI;

    public StrategySuggestionService(AzureOpenAI azureOpenAI)
    {
        _azureOpenAI = azureOpenAI;
    }

    public async Task<List<StrategySuggestion>> GetSuggestionsAsync(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<AgentChatMessage> history,
        ChatMemoryContext memoryContext,
        Guid? conversationId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = history.Select(m => new _shared.ChatMessage
        {
            Role = m.Role == AgentChatMessageRole.User
                ? _shared.ChatMessageRole.User
                : _shared.ChatMessageRole.Assistant,
            Content = m.Content
        }).ToList();

        var suggestions = await _azureOpenAI.GetStrategySuggestionsAsync(
            userQuery,
            userId,
            userInformation,
            userName,
            chatHistory,
            memoryContext,
            conversationId,
            correlationId,
            cancellationToken);

        return suggestions.Select(s => new StrategySuggestion
        {
            Title = s.Title,
            Description = s.Description,
            IconKey = s.IconKey
        }).ToList();
    }
}
