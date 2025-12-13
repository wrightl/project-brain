namespace ProjectBrain.AI;

using System.ClientModel;
using System.Linq;
using System.Text;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using _shared = Models;

// public interface IChatService
// {
//     Task<CollectionResult<StreamingChatCompletionUpdate>> GetResponse();
// }

public class AzureOpenAIServices(
        OpenAIClient openAIClient,
        ISearchIndexService searchIndexService,
        IConfiguration configuration,
        ILogger<AzureOpenAIServices> logger)
{

    public ILogger<AzureOpenAIServices> Logger { get; } = logger;
    public OpenAIClient OpenAIClient { get; } = openAIClient;
    public IConfiguration Configuration { get; } = configuration;
    public ISearchIndexService SearchIndexService { get; } = searchIndexService;
}

public class AzureOpenAI(AzureOpenAIServices services) //: IChatService
{
    public AzureOpenAIServices Services { get; } = services;

    public async Task<string> TranscribeAudio(Stream audioStream, string fileName)
    {
        Services.Logger.LogInformation("Starting TranscribeAudio for file: {FileName}", fileName);

        try
        {
            var audioClient = Services.OpenAIClient.GetAudioClient("openai-speech-deployment");

            // Reset stream position
            audioStream.Position = 0;

            var response = await audioClient.TranscribeAudioAsync(
                audioStream,
                fileName,
                new AudioTranscriptionOptions
                {
                    ResponseFormat = AudioTranscriptionFormat.Text,
                    Language = "en" // Can be made configurable if needed
                });

            var transcription = response.Value.Text;

            Services.Logger.LogInformation("Audio transcription completed. Length: {Length} characters", transcription.Length);
            return transcription;
        }
        catch (Exception ex)
        {
            Services.Logger.LogError(ex, "Error transcribing audio for file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> GetConversationSummary(string userQuery, string userId)
    {
        Services.Logger.LogInformation("Starting GetConversationSummary for userQuery: {UserQuery}", userQuery);

        try
        {
            ChatClient chatClient = Services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);
            var response = await chatClient.CompleteChatAsync(
            [
                new UserChatMessage($"Summarize this query in a short, concise title: {userQuery}")
            ]);
            return response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            Services.Logger.LogError(ex, "Error generating conversation summary for query: {UserQuery}", userQuery);
            // Fallback to a simple truncation if summary generation fails
            return userQuery.Length > 50 ? userQuery[..50] + "..." : userQuery;
        }
    }

    // public async Task<AsyncCollectionResult<StreamingChatCompletionUpdate>> GetResponse(string userQuery, string userId, string userName, List<_shared.ChatMessage> history)
    // {
    //     return await getChatResponse(userQuery, userId, userName, history);
    // }

    public async Task<(AsyncCollectionResult<StreamingChatCompletionUpdate> Response, List<CitationInfo> Citations)> GetResponseWithCitations(string userQuery, string userId, string userInformation, string userName, List<_shared.ChatMessage> history)
    {
        return await getChatResponseWithCitations(userQuery, userId, userInformation, userName, history);
    }

    // private async Task<AsyncCollectionResult<StreamingChatCompletionUpdate>> getChatResponse(string userQuery, string userId, string userName, List<_shared.ChatMessage> history)
    // {
    //     _logger.LogInformation("Starting getNewChatResponse for userQuery: {UserQuery}, userId: {UserId}, userName: {UserName}", userQuery, userId, userName);

    //     // Get configurable limits from configuration
    //     var maxSearchResults = int.Parse(_configuration["AI:MaxSearchResults"] ?? "5");
    //     var maxContentLengthPerSource = int.Parse(_configuration["AI:MaxContentLengthPerSource"] ?? "800");
    //     var maxHistoryMessages = int.Parse(_configuration["AI:MaxHistoryMessages"] ?? "10");
    //     var maxTotalTokens = int.Parse(_configuration["AI:MaxTotalTokens"] ?? "7000"); // Leave buffer for response

    //     // Vectorize the query
    //     var embedClient = _openAIClient.GetEmbeddingClient("openai-embed-deployment");
    //     var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
    //     var embedResponse = await embedClient.GenerateEmbeddingAsync(userQuery, embeddingOptions);
    //     var queryVector = embedResponse.Value.ToFloats();

    //     // Configure search options for user-specific documents
    //     var searchOptions = new SearchOptions
    //     {
    //         Size = maxSearchResults,
    //         VectorSearch = new()
    //         {
    //             Queries =
    //             {
    //                 new VectorizedQuery(queryVector)
    //                 {
    //                     KNearestNeighborsCount = maxSearchResults,
    //                     Fields = { "embedding" }
    //                 }
    //             }
    //         },
    //         Filter = $"ownerId eq '{userId.Replace("'", "''")}' or ownerId eq '' or ownerId eq null" // Filter to user's documents only
    //     };
    //     // Select specific fields
    //     searchOptions.Select.Add("id");
    //     searchOptions.Select.Add("content");
    //     searchOptions.Select.Add("sourcefile");
    //     searchOptions.Select.Add("sourcepage");
    //     searchOptions.Select.Add("storageUrl");
    //     searchOptions.Select.Add("category");

    //     _logger.LogInformation("Executing vector search for user {UserId} with query: {UserQuery}", userId, userQuery);

    //     // Execute the search
    //     var searchClient = _searchIndexClient.GetSearchClient(SEARCH_INDEX_NAME);
    //     var searchResults = await searchClient.SearchAsync<SearchDocument>(userQuery, searchOptions);

    //     _logger.LogInformation("Search results received, processing...");

    //     // Build formatted sources with citations
    //     var sourcesFormatted = new StringBuilder();
    //     var citations = new List<CitationInfo>();
    //     int citationIndex = 1;

    //     await foreach (var result in searchResults.Value.GetResultsAsync())
    //     {
    //         var doc = result.Document;
    //         var content = doc.ContainsKey("content") ? doc["content"]?.ToString() ?? "" : "";
    //         var sourceFile = doc.ContainsKey("sourcefile") ? doc["sourcefile"]?.ToString() ?? "Unknown" : "Unknown";
    //         var sourcePage = doc.ContainsKey("sourcepage") ? doc["sourcepage"]?.ToString() ?? "" : "";
    //         var storageUrl = doc.ContainsKey("storageUrl") ? doc["storageUrl"]?.ToString() ?? "" : "";
    //         var category = doc.ContainsKey("category") ? doc["category"]?.ToString() ?? "" : "";

    //         if (string.IsNullOrWhiteSpace(content))
    //             continue;

    //         // Truncate content to limit tokens
    //         if (content.Length > maxContentLengthPerSource)
    //         {
    //             content = content.Substring(0, maxContentLengthPerSource) + "... [truncated]";
    //         }

    //         // Store citation information
    //         citations.Add(new CitationInfo
    //         {
    //             Index = citationIndex,
    //             SourceFile = sourceFile,
    //             SourcePage = sourcePage,
    //             StorageUrl = storageUrl,
    //             Category = category,
    //             Content = content
    //         });

    //         // Format source with citation number
    //         sourcesFormatted.AppendLine($"[{citationIndex}] Source: {sourceFile}");
    //         if (!string.IsNullOrEmpty(sourcePage))
    //         {
    //             sourcesFormatted.AppendLine($"    Page/Section: {sourcePage}");
    //         }
    //         sourcesFormatted.AppendLine($"    Content: {content}");
    //         sourcesFormatted.AppendLine();

    //         citationIndex++;
    //     }

    //     _logger.LogInformation("Found {CitationCount} relevant sources for query", citations.Count);

    //     // Build system prompt with instructions
    //     var systemPrompt = BuildSystemPrompt(userName, citations.Count > 0);

    //     // Limit conversation history to most recent messages
    //     var limitedHistory = history.TakeLast(maxHistoryMessages).ToList();

    //     // Build the user prompt with context, sources, and instructions
    //     var userPrompt = BuildUserPrompt(userQuery, sourcesFormatted.ToString(), citations.Count, limitedHistory);

    //     _logger.LogInformation("Formatted prompt with {SourceCount} sources and {HistoryCount} history messages (limited from {OriginalHistoryCount})",
    //         citations.Count, limitedHistory.Count, history.Count);

    //     // Create chat messages using limited history
    //     var messages = ToChatMessages(limitedHistory);

    //     // Combine system prompt with user prompt and add as final user message
    //     var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

    //     // Estimate tokens (rough approximation: 1 token ≈ 4 characters)
    //     var estimatedTokens = combinedPrompt.Length / 4;

    //     // Also estimate tokens from history messages
    //     foreach (var msg in messages)
    //     {
    //         estimatedTokens += msg.Content.Count / 4;
    //     }

    //     _logger.LogInformation("Sending {MessageCount} messages to ChatClient", messages.Count);
    //     _logger.LogInformation("Prompt Length: {PromptLength}", combinedPrompt.Length);
    //     _logger.LogInformation("Estimated Total Tokens: {Tokens}", estimatedTokens);

    //     // If still over limit, truncate sources further
    //     if (estimatedTokens > maxTotalTokens)
    //     {
    //         _logger.LogWarning("Estimated tokens ({EstimatedTokens}) exceed limit ({MaxTokens}). Truncating sources further.",
    //             estimatedTokens, maxTotalTokens);

    //         // Reduce content length per source
    //         var reductionFactor = (double)maxTotalTokens / estimatedTokens;
    //         var newMaxContentLength = (int)(maxContentLengthPerSource * reductionFactor * 0.9); // 90% to be safe

    //         sourcesFormatted.Clear();
    //         citationIndex = 1;

    //         foreach (var citation in citations)
    //         {
    //             var truncatedContent = citation.Content.Length > newMaxContentLength
    //                 ? citation.Content.Substring(0, newMaxContentLength) + "... [truncated]"
    //                 : citation.Content;

    //             sourcesFormatted.AppendLine($"[{citationIndex}] Source: {citation.SourceFile}");
    //             if (!string.IsNullOrEmpty(citation.SourcePage))
    //             {
    //                 sourcesFormatted.AppendLine($"    Page/Section: {citation.SourcePage}");
    //             }
    //             sourcesFormatted.AppendLine($"    Content: {truncatedContent}");
    //             sourcesFormatted.AppendLine();
    //             citationIndex++;
    //         }

    //         // Rebuild user prompt with truncated sources
    //         userPrompt = BuildUserPrompt(userQuery, sourcesFormatted.ToString(), citations.Count, limitedHistory);
    //         combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

    //         estimatedTokens = (combinedPrompt.Length / 4) + (messages.Sum(m => m.Content.Count) / 4);
    //         _logger.LogInformation("After truncation - Estimated Total Tokens: {Tokens}", estimatedTokens);
    //     }

    //     messages.Add(new UserChatMessage(combinedPrompt));

    //     // Get streaming response using the same pattern as the working getChatResponse method
    //     var chatClient = _openAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);
    //     var response = chatClient.CompleteChatStreamingAsync(messages);

    //     return response;
    // }

    private async Task<(AsyncCollectionResult<StreamingChatCompletionUpdate> Response, List<CitationInfo> Citations)> getChatResponseWithCitations(string userQuery, string userId, string userInformation, string userName, List<_shared.ChatMessage> history)
    {
        Services.Logger.LogInformation("Starting getNewChatResponseWithCitations for userQuery: {UserQuery}, userId: {UserId}, userName: {UserName}", userQuery, userId, userName);

        // Get configurable limits from configuration
        var maxSearchResults = int.Parse(Services.Configuration["AI:MaxSearchResults"] ?? "5");
        var maxContentLengthPerSource = int.Parse(Services.Configuration["AI:MaxContentLengthPerSource"] ?? "800");
        var maxHistoryMessages = int.Parse(Services.Configuration["AI:MaxHistoryMessages"] ?? "10");
        var maxTotalTokens = int.Parse(Services.Configuration["AI:MaxTotalTokens"] ?? "7000");

        // Vectorize the query
        var embedClient = Services.OpenAIClient.GetEmbeddingClient("openai-embed-deployment");
        var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
        var embedResponse = await embedClient.GenerateEmbeddingAsync(userQuery, embeddingOptions);
        var queryVector = embedResponse.Value.ToFloats();

        // Configure search options for user-specific documents
        var searchOptions = new SearchOptions
        {
            Size = maxSearchResults,
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = maxSearchResults,
                        Fields = { "embedding" }
                    }
                }
            },
            Filter = $"ownerId eq '{userId.Replace("'", "''")}' or ownerId eq '' or ownerId eq null"
        };
        searchOptions.Select.Add("id");
        searchOptions.Select.Add("content");
        searchOptions.Select.Add("sourcefile");
        searchOptions.Select.Add("sourcepage");
        searchOptions.Select.Add("storageUrl");
        searchOptions.Select.Add("category");
        searchOptions.Select.Add("ownerId");

        Services.Logger.LogInformation("Executing vector search for user {UserId} with query: {UserQuery}", userId, userQuery);

        // Execute the search
        var searchResults = await Services.SearchIndexService.SearchAsync(userQuery, searchOptions);

        Services.Logger.LogInformation("Search results received, processing...");

        // Build formatted sources with citations
        var sourcesFormatted = new StringBuilder();
        var citations = new List<CitationInfo>();
        var citationContents = new Dictionary<int, string>(); // Store content separately for truncation
        int citationIndex = 1;

        await foreach (var result in searchResults.Value.GetResultsAsync())
        {
            var doc = result.Document;
            var id = doc.ContainsKey("id") ? doc["id"]?.ToString() ?? "" : "";
            var content = doc.ContainsKey("content") ? doc["content"]?.ToString() ?? "" : "";
            var sourceFile = doc.ContainsKey("sourcefile") ? doc["sourcefile"]?.ToString() ?? "Unknown" : "Unknown";
            var sourcePage = doc.ContainsKey("sourcepage") ? doc["sourcepage"]?.ToString() ?? "" : "";
            var storageUrl = doc.ContainsKey("storageUrl") ? doc["storageUrl"]?.ToString() ?? "" : "";
            var ownerId = doc.ContainsKey("ownerId") ? doc["ownerId"]?.ToString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(content))
                continue;

            // Store original content for potential truncation
            var originalContent = content;

            // Truncate content to limit tokens
            if (content.Length > maxContentLengthPerSource)
            {
                content = content.Substring(0, maxContentLengthPerSource) + "... [truncated]";
            }

            // Store citation information
            citations.Add(new CitationInfo
            {
                Id = id,
                Index = citationIndex,
                SourceFile = sourceFile,
                SourcePage = sourcePage,
                StorageUrl = $"{storageUrl}",
                IsShared = string.IsNullOrEmpty(ownerId) ? true : false
            });

            // Store original content for truncation purposes
            citationContents[citationIndex] = originalContent;

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

        Services.Logger.LogInformation("Found {CitationCount} relevant sources for query", citations.Count);

        // Build system prompt with instructions
        var systemPrompt = BuildSystemPrompt(userName, citations.Count > 0);

        // Limit conversation history to most recent messages
        var limitedHistory = history.TakeLast(maxHistoryMessages).ToList();

        // Build the user prompt with context, sources, and instructions
        var userPrompt = BuildUserPrompt(userQuery, userInformation, sourcesFormatted.ToString(), citations.Count, limitedHistory);

        Services.Logger.LogInformation("Formatted prompt with {SourceCount} sources and {HistoryCount} history messages (limited from {OriginalHistoryCount})",
            citations.Count, limitedHistory.Count, history.Count);

        // Create chat messages using limited history
        var messages = ToChatMessages(limitedHistory);

        // Combine system prompt with user prompt and add as final user message
        var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

        // Estimate tokens (rough approximation: 1 token ≈ 4 characters)
        var estimatedTokens = combinedPrompt.Length / 4;

        // Also estimate tokens from history messages
        foreach (var msg in messages)
        {
            estimatedTokens += msg.Content.Count / 4;
        }

        Services.Logger.LogInformation("Sending {MessageCount} messages to ChatClient", messages.Count);
        Services.Logger.LogInformation("Prompt Length: {PromptLength}", combinedPrompt.Length);
        Services.Logger.LogInformation("Estimated Total Tokens: {Tokens}", estimatedTokens);

        // If still over limit, truncate sources further
        if (estimatedTokens > maxTotalTokens)
        {
            Services.Logger.LogWarning("Estimated tokens ({EstimatedTokens}) exceed limit ({MaxTokens}). Truncating sources further.",
                estimatedTokens, maxTotalTokens);

            // Reduce content length per source
            var reductionFactor = (double)maxTotalTokens / estimatedTokens;
            var newMaxContentLength = (int)(maxContentLengthPerSource * reductionFactor * 0.9); // 90% to be safe

            sourcesFormatted.Clear();
            citationIndex = 1;

            foreach (var citation in citations)
            {
                var originalContent = citationContents[citation.Index];
                var truncatedContent = originalContent.Length > newMaxContentLength
                    ? originalContent.Substring(0, newMaxContentLength) + "... [truncated]"
                    : originalContent;

                sourcesFormatted.AppendLine($"[{citationIndex}] Source: {citation.SourceFile}");
                if (!string.IsNullOrEmpty(citation.SourcePage))
                {
                    sourcesFormatted.AppendLine($"    Page/Section: {citation.SourcePage}");
                }
                sourcesFormatted.AppendLine($"    Content: {truncatedContent}");
                sourcesFormatted.AppendLine();
                citationIndex++;
            }

            // Rebuild user prompt with truncated sources
            userPrompt = BuildUserPrompt(userQuery, userInformation, sourcesFormatted.ToString(), citations.Count, limitedHistory);
            combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

            estimatedTokens = (combinedPrompt.Length / 4) + (messages.Sum(m => m.Content.Count) / 4);
            Services.Logger.LogInformation("After truncation - Estimated Total Tokens: {Tokens}", estimatedTokens);
        }

        messages.Add(new UserChatMessage(combinedPrompt));

        // Get streaming response
        var chatClient = Services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);
        var response = chatClient.CompleteChatStreamingAsync(messages);

        return (response, citations);
    }

    // private async Task<(AsyncCollectionResult<StreamingChatCompletionUpdate> Response, List<CitationInfo> Citations)> getChatResponseWithCitations(string userQuery, string userId, string userName, List<_shared.ChatMessage> history)
    // {
    //     // For the old method, return empty citations for now
    //     // You could implement similar logic if needed
    //     var response = await getChatResponse(userQuery, userId, userName, history);
    //     return (response, new List<CitationInfo>());
    // }

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
            // prompt.AppendLine("- Base your response on the provided sources");
            prompt.AppendLine("- Base your response on the provided sources, the user's query and the conversation history");
            // prompt.AppendLine("- If sources don't fully answer the question, acknowledge this and provide what you can");
            prompt.AppendLine("- Ignore any sources that are not relevant to the user's query or conversation history");
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

    private string BuildUserPrompt(string userQuery, string userInformation, string sources, int citationCount, List<_shared.ChatMessage> history)
    {
        var prompt = new StringBuilder();

        if (citationCount > 0)
        {
            prompt.AppendLine($"Here are {citationCount} relevant sources from the user's documents:");
            prompt.AppendLine("---");
            prompt.AppendLine(sources);
            prompt.AppendLine("---");
            prompt.AppendLine("Answer the query using the sources above. Cite sources with [number] format. If the sources don't help answer the question, ignore them completely.");
            prompt.AppendLine();
        }
        // else
        // {
        //     prompt.AppendLine();
        //     prompt.AppendLine("No specific sources were found in the user's documents for this query. Provide helpful general guidance and ask if they'd like to search for more specific information.");
        // }

        prompt.AppendLine("---");
        prompt.AppendLine("Here is some data in json format about the user based on their onboarding data:");
        prompt.AppendLine(userInformation);
        prompt.AppendLine("---");

        prompt.AppendLine("User Query:");
        prompt.AppendLine(userQuery);

        if (history.Count > 0)
        {
            prompt.AppendLine();
            prompt.AppendLine("Note: Use the conversation history above for context when answering.");
        }

        return prompt.ToString();
    }

    public class CitationInfo
    {
        public string Id { get; set; } = string.Empty;
        public int Index { get; set; }
        public string SourceFile { get; set; } = string.Empty;
        public string SourcePage { get; set; } = string.Empty;
        public string StorageUrl { get; set; } = string.Empty;
        public bool IsShared { get; set; } = false;
    }

    // private async Task<AsyncCollectionResult<StreamingChatCompletionUpdate>> getChatResponse(string userQuery, string userId, string userName, List<_shared.ChatMessage> history)
    // {
    //     // Vectorise query
    //     var embedClient = _openAIClient.GetEmbeddingClient("openai-embed-deployment");
    //     var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
    //     var embedResponse = await embedClient.GenerateEmbeddingAsync(userQuery, embeddingOptions);
    //     var queryVector = embedResponse.Value.ToFloats();

    //     _logger.LogInformation("Starting GetResponse for userQuery: {UserQuery}", userQuery);

    //     // Get configurable limits
    //     var maxSearchResults = int.Parse(_configuration["AI:MaxSearchResults"] ?? "5");
    //     var maxContentLengthPerSource = int.Parse(_configuration["AI:MaxContentLengthPerSource"] ?? "800");
    //     var maxHistoryMessages = int.Parse(_configuration["AI:MaxHistoryMessages"] ?? "10");

    //     // Configure search options
    //     var searchOptions = new SearchOptions
    //     {
    //         Size = maxSearchResults,
    //         VectorSearch = new()
    //         {
    //             Queries =
    //             {
    //                 new VectorizedQuery(queryVector)
    //                 {
    //                     KNearestNeighborsCount = maxSearchResults,
    //                     Fields = { "embedding" }
    //                 }
    //             }
    //         },
    //         Filter = $"ownerId eq '{userId}'",
    //     };

    //     _logger.LogInformation("Executing search with options: {SearchOptions}", JsonSerializer.Serialize(searchOptions));

    //     // Execute the search
    //     var searchClient = _searchIndexClient.GetSearchClient(SEARCH_INDEX_NAME);
    //     var searchResults = await searchClient.SearchAsync<SearchDocument>(userQuery, searchOptions);

    //     _logger.LogInformation("Search results received, processing...");

    //     StringBuilder sourcesFormatted = new StringBuilder();
    //     await foreach (var result in searchResults.Value.GetResultsAsync())
    //     {
    //         var doc = result.Document;
    //         var title = doc.ContainsKey("sourcefile") ? doc["sourcefile"]?.ToString() ?? "Unknown" : "Unknown";
    //         var content = doc.ContainsKey("content") ? doc["content"]?.ToString() ?? "" : "";

    //         // Truncate content to limit tokens
    //         if (!string.IsNullOrEmpty(content) && content.Length > maxContentLengthPerSource)
    //         {
    //             content = content.Substring(0, maxContentLengthPerSource) + "... [truncated]";
    //         }

    //         sourcesFormatted.AppendLine($"Title: {title}");
    //         sourcesFormatted.AppendLine($"Content: {content}");
    //         sourcesFormatted.AppendLine("---");
    //         _logger.LogInformation("Search hit: {Document}", JsonSerializer.Serialize(result.Document));
    //     }

    //     // Prompt template for grounding the LLM response in search results
    //     // string GROUNDED_PROMPT_V1 = @"You are a friendly assistant that gives advice to neurodiverse individuals.
    //     //     Answer the query using only the sources provided below in a friendly and concise bulleted manner
    //     //     Answer ONLY with the facts listed in the list of sources below.
    //     //     If there isn't enough information below, say you don't know.
    //     //     Do not generate answers that don't use the sources below.
    //     //     Query: {0}
    //     //     Sources: {1}";
    //     string GROUNDED_PROMPT_V2 = @"You are a friendly assistant that gives advice to neurodiverse individuals.
    //         Answer the query using the sources provided below in a friendly to give a more accurate and contextually relevant response.
    //         If there isn't enough information below, say you don't know.
    //         Personalize the answer by using the individuals name {2}.
    //         Query: {0}
    //         Sources: {1}";


    //     string formattedPrompt = string.Format(GROUNDED_PROMPT_V2, userQuery, sourcesFormatted, userName);

    //     _logger.LogInformation("Formatted prompt for LLM: {FormattedPrompt}", formattedPrompt);

    //     ChatClient chatClient = _openAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);

    //     // Limit conversation history
    //     var limitedHistory = history.TakeLast(maxHistoryMessages).ToList();
    //     var messages = ToChatMessages(limitedHistory);
    //     messages.Add(new UserChatMessage(formattedPrompt));

    //     _logger.LogInformation($"Sending {messages.Count} messages to ChatClient: {JsonSerializer.Serialize(messages)}");

    //     var response = chatClient.CompleteChatStreamingAsync(messages);
    //     return response;
    // }

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
               .AddChatClient(deploymentName: Constants.CHAT_CLIENT_DEPLOYMENT);

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

        builder.Services.AddScoped<AzureOpenAIServices>();
        builder.Services.AddScoped<AzureOpenAI>();
        builder.Services.AddScoped<ISearchIndexService, AzureSearchClient>();
        builder.Services.AddScoped<AzureSearchClientServices>();
    }
}
