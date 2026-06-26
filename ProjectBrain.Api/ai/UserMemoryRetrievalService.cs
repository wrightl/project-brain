namespace ProjectBrain.AI;

using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using OpenAI;
using OpenAI.Embeddings;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;

public class UserMemoryRetrievalService : IUserMemoryRetrievalService
{
    private readonly ISearchIndexService _searchIndexService;
    private readonly OpenAIClient _openAIClient;
    private readonly IUserFactRepository _factRepository;
    private readonly IUserEpisodeRepository _episodeRepository;
    private readonly ILogger<UserMemoryRetrievalService> _logger;

    public UserMemoryRetrievalService(
        ISearchIndexService searchIndexService,
        OpenAIClient openAIClient,
        IUserFactRepository factRepository,
        IUserEpisodeRepository episodeRepository,
        ILogger<UserMemoryRetrievalService> logger)
    {
        _searchIndexService = searchIndexService;
        _openAIClient = openAIClient;
        _factRepository = factRepository;
        _episodeRepository = episodeRepository;
        _logger = logger;
    }

    public async Task<MemoryRetrievalResult> SearchAsync(
        string userId,
        string query,
        MemorySettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!settings.EnableMemoryFormation || string.IsNullOrWhiteSpace(query))
        {
            return new MemoryRetrievalResult { RetrievalMode = "disabled" };
        }

        try
        {
            var embedClient = _openAIClient.GetEmbeddingClient("openai-embed-deployment");
            var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
            var embedResponse = await embedClient.GenerateEmbeddingAsync(query, embeddingOptions, cancellationToken);
            var queryVector = embedResponse.Value.ToFloats();

            var escapedUserId = userId.Replace("'", "''");
            var searchOptions = new SearchOptions
            {
                Size = settings.MaxFactsRetrieved + settings.MaxEpisodesRetrieved,
                QueryType = SearchQueryType.Semantic,
                SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = "default"
                },
                Filter = $"ownerId eq '{escapedUserId}' and (memoryType eq 'fact' or memoryType eq 'episode') and status eq '{MemoryStatuses.Active}'",
                VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizedQuery(queryVector)
                        {
                            KNearestNeighborsCount = settings.MaxFactsRetrieved + settings.MaxEpisodesRetrieved,
                            Fields = { "embedding" }
                        }
                    }
                }
            };

            searchOptions.Select.Add("memoryType");
            searchOptions.Select.Add("memoryId");
            searchOptions.Select.Add("content");
            searchOptions.Select.Add("topic");
            searchOptions.Select.Add("sourcepage");

            var searchResults = await _searchIndexService.SearchAsync(query, searchOptions);
            var facts = new List<RetrievedUserFact>();
            var episodes = new List<RetrievedUserEpisode>();

            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                var doc = result.Document;
                var memoryType = doc.GetString("memoryType") ?? string.Empty;
                var memoryIdRaw = doc.GetString("memoryId") ?? doc.GetString("sourcepage") ?? string.Empty;
                if (!Guid.TryParse(memoryIdRaw, out var memoryId))
                {
                    continue;
                }

                if (memoryType == "fact" && facts.Count < settings.MaxFactsRetrieved)
                {
                    var fact = await _factRepository.GetByIdForUserAsync(memoryId, userId, cancellationToken);
                    if (fact?.Status != MemoryStatuses.Active)
                    {
                        continue;
                    }

                    facts.Add(new RetrievedUserFact
                    {
                        Id = fact.Id,
                        Content = fact.Content,
                        Category = fact.Category
                    });
                }
                else if (memoryType == "episode" && episodes.Count < settings.MaxEpisodesRetrieved)
                {
                    var episode = await _episodeRepository.GetByIdForUserAsync(memoryId, userId, cancellationToken);
                    if (episode?.Status != MemoryStatuses.Active)
                    {
                        continue;
                    }

                    episodes.Add(new RetrievedUserEpisode
                    {
                        Id = episode.Id,
                        Summary = episode.Summary,
                        Topic = episode.Topic,
                        Outcome = episode.Outcome
                    });
                }
            }

            if (facts.Count > 0 || episodes.Count > 0)
            {
                return new MemoryRetrievalResult
                {
                    RetrievalMode = "hybrid",
                    Facts = facts,
                    Episodes = episodes
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hybrid memory search failed for user {UserId}; falling back to SQL", userId);
        }

        return await SqlFallbackAsync(userId, query, settings, cancellationToken);
    }

    private async Task<MemoryRetrievalResult> SqlFallbackAsync(
        string userId,
        string query,
        MemorySettings settings,
        CancellationToken cancellationToken)
    {
        var facts = await _factRepository.SearchActiveByContentAsync(
            userId, query, settings.MaxFactsRetrieved, cancellationToken);
        var episodes = await _episodeRepository.SearchActiveByContentAsync(
            userId, query, settings.MaxEpisodesRetrieved, cancellationToken);

        return new MemoryRetrievalResult
        {
            RetrievalMode = "sql_fallback",
            Facts = facts.Select(f => new RetrievedUserFact
            {
                Id = f.Id,
                Content = f.Content,
                Category = f.Category
            }).ToList(),
            Episodes = episodes.Select(e => new RetrievedUserEpisode
            {
                Id = e.Id,
                Summary = e.Summary,
                Topic = e.Topic,
                Outcome = e.Outcome
            }).ToList()
        };
    }
}

internal static class SearchDocumentExtensions
{
    public static string? GetString(this SearchDocument doc, string key) =>
        doc.TryGetValue(key, out var value) ? value?.ToString() : null;
}
