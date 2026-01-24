

using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using ProjectBrain.AI;

// --- Background task to run AISeeding.SeedAsync on startup ---
public class AISeedingBackgroundTask : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AISeedingBackgroundTask> _logger;
    private Task? _executingTask;

    public AISeedingBackgroundTask(IServiceProvider serviceProvider, ILogger<AISeedingBackgroundTask> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _executingTask = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<AISeeding>();
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running AISeeding.SeedAsync");
            }
        }, cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class AISeeding
{
    private readonly SearchIndexClient _searchIndexClient;
    private readonly ILogger<AISeeding> _logger;

    public AISeeding(
        SearchIndexClient searchIndexClient,
        ILogger<AISeeding> logger)
    {
        _searchIndexClient = searchIndexClient;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting AI Seeding task...");

        await EnsureSearchIndexAsync(Constants.SEARCH_INDEX_NAME);

        _logger.LogInformation("AI Seeding task completed.");
    }

    public async Task CreateSearchIndexAsync(string searchIndexName, CancellationToken ct = default)
    {
        string vectorSearchConfigName = "user-resources-algorithm";
        string vectorSearchProfile = "user-resources-azureOpenAi-text-profile";
        var index = new SearchIndex(searchIndexName)
        {
            VectorSearch = new()
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(vectorSearchConfigName)
                },
                Profiles =
                {
                    new VectorSearchProfile(vectorSearchProfile, vectorSearchConfigName)
                }
            },
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("content") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SimpleField("category", SearchFieldDataType.String) { IsFacetable = true },
                new SimpleField("sourcepage", SearchFieldDataType.String) { IsFacetable = true },
                new SimpleField("sourcefile", SearchFieldDataType.String) { IsFacetable = true },
                new SimpleField("storageUrl", SearchFieldDataType.String) { IsFacetable = false, IsFilterable = true },
                new SimpleField("ownerId", SearchFieldDataType.String) { IsFacetable = false, IsFilterable = true },
                // new SearchField("oids", SearchFieldDataType.Collection(SearchFieldDataType.String))
                // {
                //      IsFacetable = true,
                // },
                new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    VectorSearchDimensions = 1536,
                    IsSearchable = true,
                    VectorSearchProfileName = vectorSearchProfile,
                }
            },
            SemanticSearch = new()
            {
                Configurations =
                {
                    new SemanticConfiguration("default", new()
                    {
                        ContentFields =
                        {
                            new SemanticField("content")
                        }
                    })
                }
            }
        };

        _logger?.LogInformation(
            "Creating '{searchIndexName}' search index", searchIndexName);

        await _searchIndexClient.CreateIndexAsync(index);
    }

    public async Task EnsureSearchIndexAsync(string searchIndexName, CancellationToken ct = default)
    {
        var indexNames = _searchIndexClient.GetIndexNamesAsync();
        await foreach (var page in indexNames.AsPages())
        {
            if (page.Values.Any(indexName => indexName == searchIndexName))
            {
                _logger?.LogWarning(
                    "Search index '{SearchIndexName}' already exists", searchIndexName);
                return;
            }
        }

        await CreateSearchIndexAsync(searchIndexName, ct);
    }
}