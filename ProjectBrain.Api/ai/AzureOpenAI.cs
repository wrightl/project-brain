namespace ProjectBrain.AI;

using System.ClientModel;
using System.Text;
using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using _shared = ProjectBrain.Models;

public interface IChatService
{
    Task<CollectionResult<StreamingChatCompletionUpdate>> GetResponse();
}

public class AzureOpenAI //: IChatService
{
    private readonly OpenAIClient _openAIClient;
    private readonly IConfiguration _configuration;
    private readonly Storage _storage;
    private readonly SearchIndexClient _searchIndexClient;

    private readonly ILogger<AzureOpenAI> _logger;

    public AzureOpenAI(
        OpenAIClient openAIClient,
        IConfiguration configuration,
        Storage storage,
        SearchIndexClient searchIndexClient,
        ILogger<AzureOpenAI> logger)
    {
        _openAIClient = openAIClient;
        _configuration = configuration;
        _storage = storage;
        _searchIndexClient = searchIndexClient;
        _logger = logger;
    }

    public async Task<string> GetConversationSummary(string userQuery, string userId)
    {
        _logger.LogInformation("Starting GetConversationSummary for userQuery: {UserQuery}", userQuery);

        ChatClient chatClient = _openAIClient.GetChatClient("openai-chat-deployment");
        var response = await chatClient.CompleteChatAsync(
        [
            new UserChatMessage($"Summarize this query in a short, concise title: {userQuery}")
        ]);
        return response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
    }

    public async Task<AsyncCollectionResult<StreamingChatCompletionUpdate>> GetResponse(string userQuery, string userId, string userName, List<_shared.ChatMessage> history)
    {
        // Vectorise query
        var embedClient = _openAIClient.GetEmbeddingClient("openai-embed-deployment");
        var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
        var embedResponse = await embedClient.GenerateEmbeddingAsync(userQuery, embeddingOptions);
        var queryVector = embedResponse.Value.ToFloats();

        _logger.LogInformation("Starting GetResponse for userQuery: {UserQuery}", userQuery);

        // Configure search options
        var searchOptions = new SearchOptions
        {
            Size = 5,
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = 5,
                        Fields = { "text_vector" }
                    }
                }
            },
            Filter = $"ownerId eq '{userId}'",
        };

        _logger.LogInformation("Executing search with options: {SearchOptions}", JsonSerializer.Serialize(searchOptions));

        // Execute the search
        var indexName = "user-resources";
        var searchClient = _searchIndexClient.GetSearchClient(indexName);
        var searchResults = await searchClient.SearchAsync<SearchDocument>(userQuery, searchOptions);

        _logger.LogInformation("Search results received, processing...");

        StringBuilder sourcesFormatted = new StringBuilder();
        await foreach (var result in searchResults.Value.GetResultsAsync())
        {
            var doc = result.Document;
            sourcesFormatted.AppendLine($"Title: {doc["title"]}");
            sourcesFormatted.AppendLine($"Content: {doc["chunk"]}");
            sourcesFormatted.AppendLine("---");
            _logger.LogInformation("Search hit: {Document}", JsonSerializer.Serialize(result.Document));
        }

        // Prompt template for grounding the LLM response in search results
        // string GROUNDED_PROMPT_V1 = @"You are a friendly assistant that gives advice to neurodiverse individuals.
        //     Answer the query using only the sources provided below in a friendly and concise bulleted manner
        //     Answer ONLY with the facts listed in the list of sources below.
        //     If there isn't enough information below, say you don't know.
        //     Do not generate answers that don't use the sources below.
        //     Query: {0}
        //     Sources: {1}";
        string GROUNDED_PROMPT_V2 = @"You are a friendly assistant that gives advice to neurodiverse individuals.
            Answer the query using the sources provided below in a friendly to give a more accurate and contextually relevant response.
            If there isn't enough information below, say you don't know.
            Personalize the answer by using the individuals name {2}.
            Query: {0}
            Sources: {1}";


        string formattedPrompt = string.Format(GROUNDED_PROMPT_V2, userQuery, sourcesFormatted, userName);

        _logger.LogInformation("Formatted prompt for LLM: {FormattedPrompt}", formattedPrompt);

        ChatClient chatClient = _openAIClient.GetChatClient("openai-chat-deployment");

        var messages = ToChatMessages(history);
        messages.Add(new UserChatMessage(formattedPrompt));

        _logger.LogInformation($"Sending {messages.Count} messages to ChatClient: {JsonSerializer.Serialize(messages)}");

        var response = chatClient.CompleteChatStreamingAsync(messages);
        return response;
    }

    private List<ChatMessage> ToChatMessages(List<_shared.ChatMessage> history)
    {
        var messages = new List<ChatMessage>();
        foreach (var msg in history)
        {
            if (msg.Role == _shared.ChatMessageRole.User)
            {
                messages.Add(new UserChatMessage(msg.Content));
            }
            else if (msg.Role == _shared.ChatMessageRole.Assistant)
            {
                messages.Add(new AssistantChatMessage(msg.Content));
            }
        }

        return messages;
    }
}

public static class AzureOpenAIExtensions
{
    public static void AddAzureOpenAI(this WebApplicationBuilder builder)
    {
        builder.AddAzureSearchClient(connectionName: "search");
        builder.AddAzureOpenAIClient(connectionName: "openai")
               .AddChatClient(deploymentName: "openai-chat-deployment");

        builder.AddAzureBlobServiceClient(connectionName: "blobs");
        builder.Services.AddScoped<Storage>();

        builder.Services.AddScoped<AzureOpenAI>();

        // builder.Services.AddTransient<BlobServiceClient>(sp =>
        // {
        //     var logger = sp.GetRequiredService<ILogger<BlobServiceClient>>();
        //     var config = sp.GetRequiredService<IConfiguration>();
        //     var connectionString = config["storage:connectionString"];
        //     logger.LogInformation("Registering BlobServiceClient with connection string: {ConnectionString}", connectionString);
        //     return new BlobServiceClient(connectionString!);
        // });
    }
}
