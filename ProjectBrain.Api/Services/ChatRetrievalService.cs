namespace ProjectBrain.Api.Services;

using ProjectBrain.AI;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;

public sealed class ChatRetrievalService : IChatRetrievalService
{
    private readonly AzureOpenAI _azureOpenAi;

    public ChatRetrievalService(AzureOpenAI azureOpenAi)
    {
        _azureOpenAi = azureOpenAi;
    }

    public async Task<ChatRetrievalResult> RetrieveAsync(
        string userQuery,
        string userId,
        ChatMemoryContext memoryContext,
        string? traceId = null,
        CancellationToken cancellationToken = default)
    {
        var (citations, sourcesFormatted, _) = await _azureOpenAi.RetrieveCitationsAsync(
            userQuery,
            userId,
            memoryContext,
            traceId,
            cancellationToken);

        return new ChatRetrievalResult
        {
            SourcesFormatted = sourcesFormatted,
            Citations = citations.Select(c => new ChatCitationDto
            {
                Id = c.Id,
                Index = c.Index,
                SourceFile = c.SourceFile,
                SourcePage = c.SourcePage,
                StorageUrl = c.StorageUrl,
                IsShared = c.IsShared
            }).ToList()
        };
    }
}
