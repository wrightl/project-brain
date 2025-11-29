

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

        await EnsureSearchIndexAsync(AzureOpenAI.SEARCH_INDEX_NAME);

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

        // if (includeImageEmbeddingsField)
        // {
        //     if (computerVisionService is null)
        //     {
        //         throw new InvalidOperationException("Computer Vision service is required to include image embeddings field");
        //     }

        //     index.Fields.Add(new SearchField("imageEmbedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
        //     {
        //         VectorSearchDimensions = computerVisionService.Dimension,
        //         IsSearchable = true,
        //         VectorSearchProfileName = vectorSearchProfile,
        //     });
        // }
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

// var fields = new List<SearchField>
//         {
//             new SearchableField("chunk_id", false)
//             {
//                 IsKey = true,
//                 IsSortable = true,
//                 IsFilterable = false,
//                 IsFacetable = false,
//                 AnalyzerName = LexicalAnalyzerName.Keyword
//             },
//             new SimpleField("parent_id", SearchFieldDataType.String)
//             {
//                 IsKey = false,
//                 IsSortable = false,
//                 IsFilterable = true,
//                 IsFacetable = false
//             },
//             new SearchableField("chunk")
//             {
//                 IsKey = false,
//                 IsFilterable = false,
//                 IsFacetable = false,
//                 IsSortable = false
//             },
//             new SearchableField("title")
//             {
//                 IsKey = false,
//                 // IsRetrievable = true,
//                 IsFilterable = false,
//                 IsFacetable = false,
//                 IsSortable = false
//             },
//             new SearchField("text_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
//             {
//                 IsKey = false,
//                 IsSearchable = true,
//                 // IsRetrievable = true,
//                 IsFilterable = false,
//                 IsFacetable = false,
//                 IsSortable = false,
//                 // Vector search settings:
//                 VectorSearchDimensions = 1536,
//                 VectorSearchProfileName = "user-resources-azureOpenAi-text-profile"
//             },
//             new SearchableField("ownerId")
//             {
//                 IsKey = false,
//                 IsSortable = false,
//                 // IsRetrievable = true,
//                 IsFilterable = true,
//                 IsFacetable = false
//             }
//         };

//         var semanticSearch = new SemanticSearch();
//         semanticSearch.DefaultConfigurationName = "user-resources-semantic-configuration";
//         semanticSearch.Configurations.Add(
//             new SemanticConfiguration("user-resources-semantic-configuration", new SemanticPrioritizedFields
//             {
//                 TitleField = new SemanticField("title"),
//                 ContentFields = { new SemanticField("chunk") }
//             }
//             )
//         );

//         var algorithm = new HnswAlgorithmConfiguration("user-resources-algorithm");
//         algorithm.Parameters = new HnswParameters();
//         algorithm.Parameters.M = 4;
//         algorithm.Parameters.EfConstruction = 400;
//         algorithm.Parameters.EfSearch = 500;
//         algorithm.Parameters.Metric = VectorSearchAlgorithmMetric.Cosine;

//         var searchProfile = new VectorSearchProfile("user-resources-azureOpenAi-text-profile", "user-resources-algorithm");
//         searchProfile.VectorizerName = "user-resources-azureOpenAi-text-vectorizer";

//         var vectoriser = new AzureOpenAIVectorizer("user-resources-azureOpenAi-text-vectorizer");
//         vectoriser.Parameters = new AzureOpenAIVectorizerParameters();
//         vectoriser.Parameters.ResourceUri = new Uri("https://projectbrain-poc.openai.azure.com");
//         vectoriser.Parameters.DeploymentName = "text-embedding-3-small";
//         vectoriser.Parameters.ModelName = "text-embedding-3-small";

//         var index = new SearchIndex("user-resources")
//         {
//             Fields = fields,
//             Similarity = new BM25Similarity(),
//             SemanticSearch = semanticSearch,
//             VectorSearch = new VectorSearch
//             {
//                 Algorithms =
//                 {
//                     algorithm
//                 },
//                 Profiles =
//                 {
//                     searchProfile
//                 },
//                 Vectorizers =
//                 {
//                     vectoriser
//                 }
//             }
//         };

//         Azure.Search.Documents.SearchClient searchClient = new Azure.Search.Documents.SearchClient();
//         searchClient.