namespace ProjectBrain.AI;

using System.ClientModel;
using System.Text;
using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using _shared = Models;

public interface IChatService
{
    Task<CollectionResult<StreamingChatCompletionUpdate>> GetResponse();
}

public class AzureOpenAI //: IChatService
{
    public const string SEARCH_INDEX_NAME = "projectbrain-documents";

    private readonly OpenAIClient _openAIClient;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAI> _logger;

    public AzureOpenAI(
        OpenAIClient openAIClient,
        SearchIndexClient searchIndexClient,
        IConfiguration configuration,
        ILogger<AzureOpenAI> logger)
    {
        _openAIClient = openAIClient;
        _searchIndexClient = searchIndexClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetConversationSummary(string userQuery, string userId)
    {
        _logger.LogInformation("Starting GetConversationSummary for userQuery: {UserQuery}", userQuery);

        try
        {
            // ChatClient chatClient = _openAIClient.GetChatClient("openai-chat-deployment");
            // var messages = new List<ChatMessage>();
            // messages.Add(new SystemChatMessage("You are an AI assistant that helps people find information."));
            // messages.Add(new UserChatMessage("hi, what's the time?"));

            // var response = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions()
            // {
            //     Temperature = (float)0.7,
            //     FrequencyPenalty = (float)0,
            //     PresencePenalty = (float)0,
            // });
            // var chatResponse = response.Value.Content.Last().Text;
            // return chatResponse;
            ChatClient chatClient = _openAIClient.GetChatClient("openai-chat-deployment");
            var response = await chatClient.CompleteChatAsync(
            [
                new UserChatMessage($"Summarize this query in a short, concise title: {userQuery}")
            ]);
            return response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
            // var messages = new List<ChatMessage>
            // {
            //     new UserChatMessage($"Summarize this query in a short, concise title: {userQuery}")
            // };

            // // Use streaming API and collect the first complete response to avoid SDK compatibility issues
            // var streamingResponse = chatClient.CompleteChatStreamingAsync(messages);
            // var summaryBuilder = new StringBuilder();

            // await foreach (var update in streamingResponse)
            // {
            //     foreach (var contentUpdate in update.ContentUpdate)
            //     {
            //         if (contentUpdate.Text != null)
            //         {
            //             summaryBuilder.Append(contentUpdate.Text);
            //         }
            //     }
            // }

            // var summary = summaryBuilder.ToString().Trim();
            // return string.IsNullOrEmpty(summary) ? (userQuery.Length > 50 ? userQuery[..50] + "..." : userQuery) : summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating conversation summary for query: {UserQuery}", userQuery);
            // Fallback to a simple truncation if summary generation fails
            return userQuery.Length > 50 ? userQuery[..50] + "..." : userQuery;
        }
    }

    public async Task<AsyncCollectionResult<StreamingChatCompletionUpdate>> GetResponse(string userQuery, string userId, string userName, List<_shared.ChatMessage> history)
    {
        var useNewSearch = _configuration["AI:UseNewSearchService"]?.ToLower() == "true";
        if (useNewSearch)
        {
            return await getNewChatResponse(userQuery, userId, userName, history);
        }
        else
        {
            return await getChatResponse(userQuery, userId, userName, history);
        }
    }

    private async Task<AsyncCollectionResult<StreamingChatCompletionUpdate>> getNewChatResponse(string userQuery, string userId, string userName, List<_shared.ChatMessage> history)
    {
        _logger.LogInformation("Starting getNewChatResponse for userQuery: {UserQuery}, userId: {UserId}, userName: {UserName}", userQuery, userId, userName);

        // Vectorize the query
        var embedClient = _openAIClient.GetEmbeddingClient("openai-embed-deployment");
        var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
        var embedResponse = await embedClient.GenerateEmbeddingAsync(userQuery, embeddingOptions);
        var queryVector = embedResponse.Value.ToFloats();

        // Configure search options for user-specific documents
        var searchOptions = new SearchOptions
        {
            Size = 8, // Get more results for better context
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = 8,
                        Fields = { "embedding" }
                    }
                }
            },
            Filter = $"ownerId eq '{userId.Replace("'", "''")}'" // Filter to user's documents only
        };
        // Select specific fields
        searchOptions.Select.Add("id");
        searchOptions.Select.Add("content");
        searchOptions.Select.Add("sourcefile");
        searchOptions.Select.Add("sourcepage");
        searchOptions.Select.Add("storageUrl");
        searchOptions.Select.Add("category");

        _logger.LogInformation("Executing vector search for user {UserId} with query: {UserQuery}", userId, userQuery);

        // Execute the search
        var searchClient = _searchIndexClient.GetSearchClient(SEARCH_INDEX_NAME);
        var searchResults = await searchClient.SearchAsync<SearchDocument>(userQuery, searchOptions);

        _logger.LogInformation("Search results received, processing...");

        // Build formatted sources with citations
        var sourcesFormatted = new StringBuilder();
        var citations = new List<CitationInfo>();
        int citationIndex = 1;

        await foreach (var result in searchResults.Value.GetResultsAsync())
        {
            var doc = result.Document;
            var content = doc.ContainsKey("content") ? doc["content"]?.ToString() ?? "" : "";
            var sourceFile = doc.ContainsKey("sourcefile") ? doc["sourcefile"]?.ToString() ?? "Unknown" : "Unknown";
            var sourcePage = doc.ContainsKey("sourcepage") ? doc["sourcepage"]?.ToString() ?? "" : "";
            var storageUrl = doc.ContainsKey("storageUrl") ? doc["storageUrl"]?.ToString() ?? "" : "";
            var category = doc.ContainsKey("category") ? doc["category"]?.ToString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(content))
                continue;

            // Store citation information
            citations.Add(new CitationInfo
            {
                Index = citationIndex,
                SourceFile = sourceFile,
                SourcePage = sourcePage,
                StorageUrl = storageUrl,
                Category = category,
                Content = content
            });

            // Format source with citation number
            sourcesFormatted.AppendLine($"[{citationIndex}] Source: {sourceFile}");
            if (!string.IsNullOrEmpty(sourcePage))
            {
                sourcesFormatted.AppendLine($"    Page/Section: {sourcePage}");
            }
            sourcesFormatted.AppendLine($"    Content: {content}");
            sourcesFormatted.AppendLine();

            citationIndex++;
        }

        _logger.LogInformation("Found {CitationCount} relevant sources for query", citations.Count);

        // Build system prompt with instructions
        var systemPrompt = BuildSystemPrompt(userName, citations.Count > 0);

        // Build the user prompt with context, sources, and instructions
        var userPrompt = BuildUserPrompt(userQuery, sourcesFormatted.ToString(), citations.Count, history);

        _logger.LogInformation("Formatted prompt with {SourceCount} sources and {HistoryCount} history messages", citations.Count, history.Count);

        // Create chat messages using the same pattern as getChatResponse which works correctly
        // This avoids SDK compatibility issues with AzureChatClient.PostfixClearStreamOptions
        var messages = ToChatMessages(history);

        // Combine system prompt with user prompt and add as final user message
        // This avoids SystemChatMessage which may cause compatibility issues
        var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";
        messages.Add(new UserChatMessage(combinedPrompt));

        _logger.LogInformation("Sending {MessageCount} messages to ChatClient", messages.Count);

        // Get streaming response using the same pattern as the working getChatResponse method
        var chatClient = _openAIClient.GetChatClient("openai-chat-deployment");
        var response = chatClient.CompleteChatStreamingAsync(messages);

        return response;
    }

    private string BuildSystemPrompt(string userName, bool hasSources)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("You are a friendly and supportive assistant helping neurodiverse individuals.");
        prompt.AppendLine("Provide helpful, clear, and empathetic responses that are easy to understand.");
        prompt.AppendLine();

        // Use name naturally, not patronizingly
        if (!string.IsNullOrWhiteSpace(userName))
        {
            prompt.AppendLine($"You are chatting with {userName}. Use their name occasionally and naturally - not in every sentence, and never in a patronizing or condescending way.");
        }

        prompt.AppendLine();
        prompt.AppendLine("Communication style:");
        prompt.AppendLine("- Be clear, concise, and break down complex information into manageable parts");
        prompt.AppendLine("- Use a friendly, supportive, and respectful tone");
        prompt.AppendLine("- If the query is unclear or ambiguous, politely ask for clarification");
        prompt.AppendLine("- Always cite sources using [number] format (e.g., [1], [2]) when referencing documents");

        if (hasSources)
        {
            prompt.AppendLine("- Base your response on the provided sources");
            prompt.AppendLine("- If sources don't fully answer the question, acknowledge this and provide what you can");
        }
        else
        {
            prompt.AppendLine("- No specific sources were found, but provide helpful general guidance");
        }

        prompt.AppendLine();
        prompt.AppendLine("Response structure:");
        prompt.AppendLine("- Answer the user's query clearly and thoroughly");
        prompt.AppendLine("- At the end, suggest 2-3 relevant follow-up questions that might help");
        prompt.AppendLine("- If clarification is needed, ask naturally within your response");

        return prompt.ToString();
    }

    private string BuildUserPrompt(string userQuery, string sources, int citationCount, List<_shared.ChatMessage> history)
    {
        var prompt = new StringBuilder();

        if (citationCount > 0)
        {
            prompt.AppendLine($"Here are {citationCount} relevant sources from the user's documents:");
            prompt.AppendLine("---");
            prompt.AppendLine(sources);
            prompt.AppendLine("---");
            prompt.AppendLine();
            prompt.AppendLine("User Query:");
            prompt.AppendLine(userQuery);
            prompt.AppendLine();
            prompt.AppendLine("Answer the query using the sources above. Cite sources with [number] format. If the sources don't fully answer the question, acknowledge this and provide what you can.");
        }
        else
        {
            prompt.AppendLine("User Query:");
            prompt.AppendLine(userQuery);
            prompt.AppendLine();
            prompt.AppendLine("No specific sources were found in the user's documents for this query. Provide helpful general guidance and ask if they'd like to search for more specific information.");
        }

        if (history.Count > 0)
        {
            prompt.AppendLine();
            prompt.AppendLine("Note: Use the conversation history above for context when answering.");
        }

        return prompt.ToString();
    }

    private class CitationInfo
    {
        public int Index { get; set; }
        public string SourceFile { get; set; } = string.Empty;
        public string SourcePage { get; set; } = string.Empty;
        public string StorageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private async Task<AsyncCollectionResult<StreamingChatCompletionUpdate>> getChatResponse(string userQuery, string userId, string userName, List<_shared.ChatMessage> history)
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
                        Fields = { "embedding" }
                    }
                }
            },
            Filter = $"ownerId eq '{userId}'",
        };

        _logger.LogInformation("Executing search with options: {SearchOptions}", JsonSerializer.Serialize(searchOptions));

        // Execute the search
        var searchClient = _searchIndexClient.GetSearchClient(SEARCH_INDEX_NAME);
        var searchResults = await searchClient.SearchAsync<SearchDocument>(userQuery, searchOptions);

        _logger.LogInformation("Search results received, processing...");

        StringBuilder sourcesFormatted = new StringBuilder();
        await foreach (var result in searchResults.Value.GetResultsAsync())
        {
            var doc = result.Document;
            var title = doc.ContainsKey("sourcefile") ? doc["sourcefile"].ToString() : "Unknown";
            var content = doc.ContainsKey("content") ? doc["content"].ToString() : "";
            sourcesFormatted.AppendLine($"Title: {title}");
            sourcesFormatted.AppendLine($"Content: {content}");
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

        // Register document embedders as singletons (they are stateless)
        builder.Services.AddSingleton<Embedding.TextDocumentEmbedder>();
        builder.Services.AddSingleton<Embedding.MarkdownDocumentEmbedder>();
        builder.Services.AddSingleton<Embedding.HtmlDocumentEmbedder>();
        builder.Services.AddSingleton<Embedding.PdfDocumentEmbedder>();
        builder.Services.AddSingleton<Embedding.DocxDocumentEmbedder>();
        builder.Services.AddSingleton<Embedding.XlsxDocumentEmbedder>();
        builder.Services.AddSingleton<Embedding.PptxDocumentEmbedder>();
        builder.Services.AddSingleton<Embedding.PngDocumentEmbedder>();

        // Register factory as singleton
        builder.Services.AddSingleton<Embedding.DocumentEmbedderFactory>(sp =>
        {
            var embedders = new List<Embedding.IDocumentEmbedder>
            {
                sp.GetRequiredService<Embedding.TextDocumentEmbedder>(),
                sp.GetRequiredService<Embedding.MarkdownDocumentEmbedder>(),
                sp.GetRequiredService<Embedding.HtmlDocumentEmbedder>(),
                sp.GetRequiredService<Embedding.PdfDocumentEmbedder>(),
                sp.GetRequiredService<Embedding.DocxDocumentEmbedder>(),
                sp.GetRequiredService<Embedding.XlsxDocumentEmbedder>(),
                sp.GetRequiredService<Embedding.PptxDocumentEmbedder>(),
                sp.GetRequiredService<Embedding.PngDocumentEmbedder>()
            };
            var logger = sp.GetRequiredService<ILogger<Embedding.DocumentEmbedderFactory>>();
            return new Embedding.DocumentEmbedderFactory(embedders, logger);
        });

        builder.Services.AddScoped<Storage>();

        builder.Services.AddScoped<AzureOpenAI>();
    }
}
