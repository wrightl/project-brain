namespace ProjectBrain.AI;

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using OpenAI;
using OpenAI.Embeddings;
using ProjectBrain.Domain;

public class UserMemoryIndexService : IUserMemoryIndexService
{
    private readonly SearchIndexClient _searchIndexClient;
    private readonly OpenAIClient _openAIClient;
    private readonly ILogger<UserMemoryIndexService> _logger;

    public UserMemoryIndexService(
        SearchIndexClient searchIndexClient,
        OpenAIClient openAIClient,
        ILogger<UserMemoryIndexService> logger)
    {
        _searchIndexClient = searchIndexClient;
        _openAIClient = openAIClient;
        _logger = logger;
    }

    public Task IndexFactAsync(UserFact fact, CancellationToken cancellationToken = default)
    {
        if (fact.Status != MemoryStatuses.Active && fact.Status != MemoryStatuses.Provisional)
        {
            return Task.CompletedTask;
        }

        return IndexMemoryAsync(
            BuildDocumentId("fact", fact.Id),
            fact.Content,
            memoryType: "fact",
            memoryId: fact.Id.ToString(),
            topic: fact.Category,
            status: fact.Status,
            ownerId: fact.UserId,
            cancellationToken);
    }

    public Task IndexEpisodeAsync(UserEpisode episode, CancellationToken cancellationToken = default)
    {
        if (episode.Status != MemoryStatuses.Active && episode.Status != MemoryStatuses.Provisional)
        {
            return Task.CompletedTask;
        }

        return IndexMemoryAsync(
            BuildDocumentId("episode", episode.Id),
            episode.Summary,
            memoryType: "episode",
            memoryId: episode.Id.ToString(),
            topic: episode.Topic,
            status: episode.Status,
            ownerId: episode.UserId,
            cancellationToken);
    }

    public async Task DeleteFactAsync(Guid factId, CancellationToken cancellationToken = default)
    {
        await DeleteDocumentAsync(BuildDocumentId("fact", factId), cancellationToken);
    }

    public async Task DeleteEpisodeAsync(Guid episodeId, CancellationToken cancellationToken = default)
    {
        await DeleteDocumentAsync(BuildDocumentId("episode", episodeId), cancellationToken);
    }

    public Task DeleteAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private async Task IndexMemoryAsync(
        string documentId,
        string content,
        string memoryType,
        string memoryId,
        string topic,
        string status,
        string ownerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var embedClient = _openAIClient.GetEmbeddingClient("openai-embed-deployment");
            var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
            var embedResponse = await embedClient.GenerateEmbeddingAsync(content, embeddingOptions, cancellationToken);
            var embedding = embedResponse.Value.ToFloats().ToArray().Select(f => (float)f).ToList();

            var searchDocument = new SearchDocument
            {
                ["id"] = documentId,
                ["content"] = content,
                ["category"] = memoryType,
                ["sourcefile"] = memoryType,
                ["sourcepage"] = memoryId,
                ["storageUrl"] = string.Empty,
                ["ownerId"] = ownerId,
                ["memoryType"] = memoryType,
                ["memoryId"] = memoryId,
                ["topic"] = topic,
                ["status"] = status,
                ["embedding"] = embedding
            };

            var searchClient = _searchIndexClient.GetSearchClient(Constants.SEARCH_INDEX_NAME);
            var batch = IndexDocumentsBatch.MergeOrUpload(new[] { searchDocument });
            await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            _logger.LogInformation("Indexed {MemoryType} memory {MemoryId} for user {UserId}", memoryType, memoryId, ownerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index {MemoryType} memory {MemoryId}", memoryType, memoryId);
        }
    }

    private async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken)
    {
        try
        {
            var searchClient = _searchIndexClient.GetSearchClient(Constants.SEARCH_INDEX_NAME);
            var batch = IndexDocumentsBatch.Delete("id", new[] { documentId });
            await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete memory document {DocumentId}", documentId);
        }
    }

    private static string BuildDocumentId(string prefix, Guid id) => $"{prefix}-{id:N}";
}
