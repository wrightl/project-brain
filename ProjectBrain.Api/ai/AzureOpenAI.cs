namespace ProjectBrain.AI;

using System.Diagnostics;
using System.ClientModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using ProjectBrain.Domain;
using _shared = Models;

// public interface IChatService
// {
//     Task<CollectionResult<StreamingChatCompletionUpdate>> GetResponse();
// }

public class AzureOpenAIServices(
        OpenAIClient openAIClient,
        ISearchIndexService searchIndexService,
        IConfiguration configuration,
        IApplicationSettingsService applicationSettingsService,
        ILogger<AzureOpenAIServices> logger)
{

    public ILogger<AzureOpenAIServices> Logger { get; } = logger;
    public OpenAIClient OpenAIClient { get; } = openAIClient;
    public IConfiguration Configuration { get; } = configuration;
    public IApplicationSettingsService ApplicationSettingsService { get; } = applicationSettingsService;
    public ISearchIndexService SearchIndexService { get; } = searchIndexService;
}

public class AzureOpenAI(AzureOpenAIServices services) //: IChatService
{
    public AzureOpenAIServices Services { get; } = services;

    private static void LogChatRagPhase(ILogger logger, string correlationId, Stopwatch sw, string phase, int? count = null)
    {
        if (count is { } c)
        {
            logger.LogInformation(
                "[ChatRag] phase={Phase} correlationId={CorrelationId} elapsedMs={ElapsedMs} count={Count}",
                phase, correlationId, sw.ElapsedMilliseconds, c);
        }
        else
        {
            logger.LogInformation(
                "[ChatRag] phase={Phase} correlationId={CorrelationId} elapsedMs={ElapsedMs}",
                phase, correlationId, sw.ElapsedMilliseconds);
        }
    }

    public record StrategySuggestion(
        string Title,
        string Description,
        string? IconKey,
        string? ArticleUrl);

    private sealed class StrategySuggestionsResponse
    {
        public List<StrategySuggestion> Items { get; init; } = new();
    }

    private sealed class DailyGoalsSuggestionResponse
    {
        public List<string> Goals { get; set; } = new();
    }

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
        Services.Logger.LogInformation(
            "Starting GetConversationSummary for user {UserId} with query length {QueryLength}",
            userId,
            userQuery.Length);

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
            Services.Logger.LogError(ex, "Error generating conversation summary for user {UserId}", userId);
            // Fallback to a simple truncation if summary generation fails
            return userQuery.Length > 50 ? userQuery[..50] + "..." : userQuery;
        }
    }

    public async Task<(AsyncCollectionResult<StreamingChatCompletionUpdate> Response, List<CitationInfo> Citations)> GetResponseWithCitations(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<_shared.ChatMessage> history,
        string? traceId = null)
    {
        return await getChatResponseWithCitations(userQuery, userId, userInformation, userName, history, traceId);
    }

    public async Task<List<StrategySuggestion>> GetStrategySuggestionsAsync(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<_shared.ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        Services.Logger.LogInformation("Generating strategy suggestions for user {UserId}", userId);

        var chatClient = Services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);

        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine("You are a friendly and supportive assistant helping neurodiverse individuals.");
        systemPrompt.AppendLine("Your job is to suggest practical coping strategies that are safe, gentle, and actionable.");
        systemPrompt.AppendLine("Avoid medical claims. If the user describes immediate danger, encourage seeking urgent professional help.");
        systemPrompt.AppendLine();
        if (!string.IsNullOrWhiteSpace(userName))
        {
            systemPrompt.AppendLine($"You are chatting with {userName}. Use their name occasionally and naturally.");
            systemPrompt.AppendLine();
        }
        systemPrompt.AppendLine("Return ONLY valid JSON. No markdown. No extra text.");
        systemPrompt.AppendLine("Return exactly 3 items in this shape:");
        systemPrompt.AppendLine("{\"items\":[{\"title\":\"...\",\"description\":\"...\",\"iconKey\":\"sparkles|lightbulb|null\",\"articleUrl\":\"https://...|null\"}]}");
        systemPrompt.AppendLine("Constraints:");
        systemPrompt.AppendLine("- titles <= 60 chars");
        systemPrompt.AppendLine("- descriptions <= 280 chars");
        systemPrompt.AppendLine("- descriptions must be specific steps the user can try");
        systemPrompt.AppendLine("- articleUrl must be https and from one of these domains (or null): nhs.uk, apa.org, mind.org.uk, helpguide.org");

        var userPrompt = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(userInformation))
        {
            userPrompt.AppendLine("---");
            userPrompt.AppendLine("User onboarding data (json):");
            userPrompt.AppendLine(userInformation);
            userPrompt.AppendLine("---");
            userPrompt.AppendLine();
        }

        if (history.Count > 0)
        {
            userPrompt.AppendLine("Conversation context:");
            foreach (var msg in history.TakeLast(6))
            {
                userPrompt.AppendLine($"- {msg.Role}: {msg.Content}");
            }
            userPrompt.AppendLine();
        }

        userPrompt.AppendLine("User input:");
        userPrompt.AppendLine(userQuery);
        userPrompt.AppendLine();
        userPrompt.AppendLine("Now return 3 coping strategy suggestions as JSON only.");

        ClientResult<ChatCompletion> response;
        try
        {
            response = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(systemPrompt.ToString()),
                    new UserChatMessage(userPrompt.ToString()),
                ],
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Common cause: identity calling Azure OpenAI lacks the data-plane action:
            // Microsoft.CognitiveServices/accounts/OpenAI/deployments/chat/completions/action
            Services.Logger.LogError(ex, "Failed to call Azure OpenAI for strategy suggestions. Falling back to defaults.");
            return new List<StrategySuggestion>();
        }

        var raw = response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<StrategySuggestion>();
        }

        Services.Logger.LogInformation("Raw response from strategies endpoint: {Raw}", raw);

        var extracted = ExtractLikelyJson(raw);
        try
        {
            var parsed = JsonSerializer.Deserialize<StrategySuggestionsResponse>(
                extracted,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var items = (parsed?.Items ?? new List<StrategySuggestion>())
                .Where(i => !string.IsNullOrWhiteSpace(i.Title) && !string.IsNullOrWhiteSpace(i.Description))
                .Select(i => i with
                {
                    IconKey = NormalizeNullableString(i.IconKey),
                    ArticleUrl = NormalizeAllowlistedArticleUrl(i.ArticleUrl)
                })
                .Take(3)
                .ToList();

            if (items.Count == 3)
            {
                return items;
            }

            return items.Take(3).ToList();
        }
        catch (Exception ex)
        {
            Services.Logger.LogWarning(ex, "Failed to parse strategy suggestion JSON. Raw: {Raw}", raw);
            return new List<StrategySuggestion>();
        }
    }

    /// <summary>
    /// Produces up to 3 suggested daily goal strings (max 500 chars each). Uses backlog and/or onboarding context.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetSuggestedDailyGoalsAsync(
        string userId,
        string userName,
        string userInformation,
        IReadOnlyList<IncompleteGoalBacklogItem> backlog,
        IReadOnlyList<string> todaysExistingGoalMessages,
        CancellationToken cancellationToken = default)
    {
        Services.Logger.LogInformation("Generating daily goal suggestions for user {UserId}", userId);

        var chatClient = Services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);
        var hasBacklog = backlog.Count > 0;

        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine("You are a friendly, supportive coach for neurodiverse people.");
        systemPrompt.AppendLine("Propose small, concrete daily goals the user could realistically attempt in one day.");
        systemPrompt.AppendLine("Avoid medical or diagnostic claims. If content implies crisis, suggest reaching out to appropriate support.");
        systemPrompt.AppendLine("Return ONLY valid JSON. No markdown fences. No extra commentary.");
        systemPrompt.AppendLine("Shape: {\"goals\":[\"...\",\"...\",\"...\"]} — exactly 3 non-empty strings.");
        systemPrompt.AppendLine("Each goal must be at most 500 characters. Use clear, encouraging language.");

        var userPrompt = BuildDailyGoalsUserPrompt(userName, userInformation, backlog, todaysExistingGoalMessages, strictThree: false);

        var goals = await CompleteAndParseDailyGoalsAsync(chatClient, systemPrompt.ToString(), userPrompt, cancellationToken);
        goals = DeduplicateAndCapGoals(goals, todaysExistingGoalMessages);

        if (goals.Count < 3)
        {
            var strictPrompt = BuildDailyGoalsUserPrompt(userName, userInformation, backlog, todaysExistingGoalMessages, strictThree: true);
            var second = await CompleteAndParseDailyGoalsAsync(chatClient, systemPrompt.ToString(), strictPrompt, cancellationToken);
            goals = MergeGoalLists(goals, second, todaysExistingGoalMessages);
        }

        if (goals.Count < 3 && hasBacklog)
        {
            foreach (var item in backlog)
            {
                if (goals.Count >= 3)
                {
                    break;
                }

                var g = TruncateGoalText(item.Message);
                if (string.IsNullOrEmpty(g) || GoalConflictsWithExisting(g, todaysExistingGoalMessages) || goals.Any(x => string.Equals(x, g, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                goals.Add(g);
            }
        }

        return goals.Take(3).ToList();
    }

    private static string BuildDailyGoalsUserPrompt(
        string userName,
        string userInformation,
        IReadOnlyList<IncompleteGoalBacklogItem> backlog,
        IReadOnlyList<string> todaysExistingGoalMessages,
        bool strictThree)
    {
        var userPrompt = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(userName))
        {
            userPrompt.AppendLine($"The user's first name is {userName}.");
            userPrompt.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(userInformation))
        {
            userPrompt.AppendLine("---");
            userPrompt.AppendLine("User onboarding / profile (markdown):");
            userPrompt.AppendLine(userInformation);
            userPrompt.AppendLine("---");
            userPrompt.AppendLine();
        }

        var blocked = todaysExistingGoalMessages
            .Select(m => m.Trim())
            .Where(m => m.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (blocked.Count > 0)
        {
            userPrompt.AppendLine("The user already has these goals planned for today — do not duplicate or closely paraphrase them:");
            foreach (var m in blocked)
            {
                userPrompt.AppendLine($"- {m}");
            }

            userPrompt.AppendLine();
        }

        if (backlog.Count > 0)
        {
            userPrompt.AppendLine("Previously incomplete goals (most important first). Each line: message; times missed; last missed date (UTC calendar day):");
            foreach (var item in backlog)
            {
                userPrompt.AppendLine($"- {item.Message}; missCount={item.MissCount}; lastMissed={item.LastMissedDate:O}");
            }

            userPrompt.AppendLine();
            userPrompt.AppendLine("Suggest 3 goals for TODAY grounded in this list (you may lightly rephrase for clarity).");
        }
        else
        {
            userPrompt.AppendLine("The user has no prior incomplete goals on record.");
            userPrompt.AppendLine("Suggest 3 gentle, practical daily goals using only the profile information above.");
        }

        if (strictThree)
        {
            userPrompt.AppendLine();
            userPrompt.AppendLine("CRITICAL: The goals array must contain exactly 3 distinct, non-empty strings.");
        }

        userPrompt.AppendLine();
        userPrompt.AppendLine("Respond with JSON only.");

        return userPrompt.ToString();
    }

    private async Task<List<string>> CompleteAndParseDailyGoalsAsync(
        ChatClient chatClient,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        ClientResult<ChatCompletion> response;
        try
        {
            response = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt),
                ],
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Services.Logger.LogError(ex, "Azure OpenAI call failed for daily goal suggestions.");
            return new List<string>();
        }

        var raw = response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<string>();
        }

        var extracted = ExtractLikelyJson(raw);
        try
        {
            var parsed = JsonSerializer.Deserialize<DailyGoalsSuggestionResponse>(
                extracted,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var list = parsed?.Goals ?? new List<string>();
            return list
                .Select(TruncateGoalText)
                .Where(g => !string.IsNullOrEmpty(g))
                .ToList();
        }
        catch (Exception ex)
        {
            Services.Logger.LogWarning(ex, "Failed to parse daily goals JSON. Raw: {Raw}", raw);
            return new List<string>();
        }
    }

    private static List<string> DeduplicateAndCapGoals(
        IReadOnlyList<string> goals,
        IReadOnlyList<string> todaysExistingGoalMessages)
    {
        var result = new List<string>();
        foreach (var g in goals)
        {
            if (result.Count >= 3)
            {
                break;
            }

            if (GoalConflictsWithExisting(g, todaysExistingGoalMessages))
            {
                continue;
            }

            if (result.Any(x => string.Equals(x, g, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            result.Add(g);
        }

        return result;
    }

    private static List<string> MergeGoalLists(
        IReadOnlyList<string> first,
        IReadOnlyList<string> second,
        IReadOnlyList<string> todaysExistingGoalMessages)
    {
        var merged = DeduplicateAndCapGoals(first, todaysExistingGoalMessages);
        foreach (var g in second)
        {
            if (merged.Count >= 3)
            {
                break;
            }

            if (GoalConflictsWithExisting(g, todaysExistingGoalMessages))
            {
                continue;
            }

            if (merged.Any(x => string.Equals(x, g, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            merged.Add(g);
        }

        return merged;
    }

    private static bool GoalConflictsWithExisting(string goal, IReadOnlyList<string> todaysExistingGoalMessages)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            return true;
        }

        var g = goal.Trim();
        return todaysExistingGoalMessages.Any(
            e => !string.IsNullOrWhiteSpace(e) &&
                 string.Equals(e.Trim(), g, StringComparison.OrdinalIgnoreCase));
    }

    private static string TruncateGoalText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var t = text.Trim();
        const int max = 500;
        return t.Length <= max ? t : t[..max];
    }

    private static string? NormalizeNullableString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Equals(value.Trim(), "null", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();
    }

    private string? NormalizeAllowlistedArticleUrl(string? url)
    {
        var normalized = NormalizeNullableString(url);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var allowlist = GetStrategyArticleHostAllowlist();
        var host = uri.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var allowed = allowlist.Any(allowedHost => HostMatches(host, allowedHost));
        return allowed ? normalized : null;
    }

    private IReadOnlyList<string> GetStrategyArticleHostAllowlist()
    {
        // Optional config override:
        // AI:StrategyArticleLinkAllowlistHosts="nhs.uk,apa.org,mind.org.uk,helpguide.org"
        var configured = Services.Configuration["AI:StrategyArticleLinkAllowlistHosts"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured
                .Split(
                    new[] { ',', ';', ' ', '\n', '\r', '\t' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }

        return new[]
        {
            "nhs.uk",
            "apa.org",
            "mind.org.uk",
            "helpguide.org"
        };
    }

    private static bool HostMatches(string host, string allowedHost)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(allowedHost))
        {
            return false;
        }

        host = host.Trim().TrimEnd('.');
        allowedHost = allowedHost.Trim().TrimEnd('.');

        if (string.Equals(host, allowedHost, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return host.EndsWith("." + allowedHost, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractLikelyJson(string input)
    {
        var trimmed = input.Trim();

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return trimmed;
    }

    private async Task<(AsyncCollectionResult<StreamingChatCompletionUpdate> Response, List<CitationInfo> Citations)> getChatResponseWithCitations(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<_shared.ChatMessage> history,
        string? traceId)
    {
        var correlationId = string.IsNullOrEmpty(traceId) ? "no-trace" : traceId;
        var sw = Stopwatch.StartNew();
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_begin");

        Services.Logger.LogInformation(
            "Starting getNewChatResponseWithCitations for user {UserId} with query length {QueryLength} and user name present: {HasUserName}",
            userId,
            userQuery.Length,
            !string.IsNullOrWhiteSpace(userName));

        // Get configurable limits from application settings (with fallback to configuration)
        AISettings aiSettings;
        try
        {
            aiSettings = await Services.ApplicationSettingsService.GetAISettingsAsync();
        }
        catch
        {
            // Fallback to configuration if service fails
            aiSettings = new AISettings
            {
                MaxSearchResults = int.Parse(Services.Configuration["AI:MaxSearchResults"] ?? "5"),
                MaxContentLengthPerSource = int.Parse(Services.Configuration["AI:MaxContentLengthPerSource"] ?? "800"),
                MaxHistoryMessages = int.Parse(Services.Configuration["AI:MaxHistoryMessages"] ?? "10"),
                MaxTotalTokens = int.Parse(Services.Configuration["AI:MaxTotalTokens"] ?? "7000")
            };
        }

        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_after_get_ai_settings");

        var maxSearchResults = aiSettings.MaxSearchResults;
        var maxContentLengthPerSource = aiSettings.MaxContentLengthPerSource;
        var maxHistoryMessages = aiSettings.MaxHistoryMessages;
        var maxTotalTokens = aiSettings.MaxTotalTokens;

        // Vectorize the query
        var embedClient = Services.OpenAIClient.GetEmbeddingClient("openai-embed-deployment");
        var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
        var embedResponse = await embedClient.GenerateEmbeddingAsync(userQuery, embeddingOptions);
        var queryVector = embedResponse.Value.ToFloats();
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_after_embedding");

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

        Services.Logger.LogInformation("Executing vector search for user {UserId} with query length {QueryLength}", userId, userQuery.Length);

        // Execute the search
        var searchResults = await Services.SearchIndexService.SearchAsync(userQuery, searchOptions);
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_after_search_async");

        Services.Logger.LogInformation("Search results received, processing...");

        // Build formatted sources with citations
        var sourcesFormatted = new StringBuilder();
        var citations = new List<CitationInfo>();
        var citationContents = new Dictionary<int, string>(); // Store content separately for truncation
        int citationIndex = 1;

        var gotFirstSearchResult = false;
        await foreach (var result in searchResults.Value.GetResultsAsync())
        {
            if (!gotFirstSearchResult)
            {
                LogChatRagPhase(Services.Logger, correlationId, sw, "rag_after_first_search_result");
                gotFirstSearchResult = true;
            }
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

        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_after_enumerate_search_results", citations.Count);

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
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_before_complete_chat_streaming", messages.Count);
        var response = chatClient.CompleteChatStreamingAsync(messages);
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_returning_stream");

        return (response, citations);
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
        builder.AddAzureSearchClient(connectionName: "ai-search");
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
        builder.Services.AddSingleton<Embedding.JsonDocumentEmbedder>();

        // Register AgentOpenAIService (implements IAgentOpenAIService from Domain)
        builder.Services.AddScoped<ProjectBrain.Domain.IAgentOpenAIService>(sp =>
        {
            var services = new AzureOpenAIServices(
                sp.GetRequiredService<OpenAIClient>(),
                sp.GetRequiredService<ISearchIndexService>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IApplicationSettingsService>(),
                sp.GetRequiredService<ILogger<AzureOpenAIServices>>());
            return new AgentOpenAIService(services);
        });

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
                sp.GetRequiredService<Embedding.PngDocumentEmbedder>(),
                sp.GetRequiredService<Embedding.JsonDocumentEmbedder>()
            };
            var logger = sp.GetRequiredService<ILogger<Embedding.DocumentEmbedderFactory>>();
            return new Embedding.DocumentEmbedderFactory(embedders, logger);
        });

        builder.Services.AddScoped<Storage>();

        builder.Services.AddScoped<AzureOpenAIServices>();
        builder.Services.AddScoped<AzureOpenAI>();
        builder.Services.AddScoped<IGoalDailySuggestionClient, GoalDailySuggestionClient>();
        builder.Services.AddScoped<ISearchIndexService, AzureSearchClient>();
        builder.Services.AddScoped<AzureSearchClientServices>();
    }
}
