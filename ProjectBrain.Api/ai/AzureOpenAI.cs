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
using ProjectBrain.Domain.Dtos;
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
        IUserMemoryService userMemoryService,
        ILogger<AzureOpenAIServices> logger)
{

    public ILogger<AzureOpenAIServices> Logger { get; } = logger;
    public OpenAIClient OpenAIClient { get; } = openAIClient;
    public IConfiguration Configuration { get; } = configuration;
    public IApplicationSettingsService ApplicationSettingsService { get; } = applicationSettingsService;
    public ISearchIndexService SearchIndexService { get; } = searchIndexService;
    public IUserMemoryService UserMemoryService { get; } = userMemoryService;
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

    public async Task<string> UpdateConversationContextSummaryAsync(
        string? existingSummary,
        IReadOnlyList<global::ChatMessage> newMessages,
        int maxSummaryLength,
        CancellationToken cancellationToken = default)
    {
        if (newMessages.Count == 0)
        {
            return existingSummary ?? string.Empty;
        }

        Services.Logger.LogInformation(
            "Updating conversation context summary with {MessageCount} new messages (existing length: {ExistingLength})",
            newMessages.Count,
            existingSummary?.Length ?? 0);

        try
        {
            var chatClient = Services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);
            var transcript = new StringBuilder();
            foreach (var message in newMessages)
            {
                transcript.AppendLine($"{message.Role}: {message.Content}");
            }

            var userPrompt = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(existingSummary))
            {
                userPrompt.AppendLine("Existing summary:");
                userPrompt.AppendLine(existingSummary);
                userPrompt.AppendLine();
                userPrompt.AppendLine("New conversation turns:");
            }
            else
            {
                userPrompt.AppendLine("Conversation turns:");
            }

            userPrompt.AppendLine(transcript.ToString());
            userPrompt.AppendLine();
            userPrompt.AppendLine($"Produce an updated rolling summary (max {maxSummaryLength} characters). Preserve important facts, topics, decisions, and emotional context. Omit filler and greetings. Return only the summary text.");

            var response = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(
                        "You compress chat transcripts into concise rolling summaries for an AI assistant. Output plain text only."),
                    new UserChatMessage(userPrompt.ToString())
                ],
                cancellationToken: cancellationToken);

            var summary = response.Value.Content.FirstOrDefault()?.Text?.Trim() ?? string.Empty;
            if (summary.Length > maxSummaryLength)
            {
                summary = summary[..maxSummaryLength];
            }

            return summary;
        }
        catch (Exception ex)
        {
            Services.Logger.LogError(ex, "Error updating conversation context summary");
            return existingSummary ?? string.Empty;
        }
    }

    public async Task<MemoryExtractionResult> ExtractMemoryCandidatesAsync(
        string userMessage,
        string assistantMessage,
        string? conversationSummary,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatClient = Services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);
            var prompt = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(conversationSummary))
            {
                prompt.AppendLine("Conversation context:");
                prompt.AppendLine(conversationSummary);
                prompt.AppendLine();
            }

            prompt.AppendLine("Latest turn:");
            prompt.AppendLine($"User: {userMessage}");
            prompt.AppendLine($"Assistant: {assistantMessage}");
            prompt.AppendLine();
            prompt.AppendLine("""
                Extract durable memory candidates from this turn. Return ONLY valid JSON:
                {"facts":[{"content":"...","category":"preference|work_context|trigger|general","confidence":0.0}],"episodes":[{"summary":"...","topic":"...","outcome":"helpful|neutral|unhelpful|unknown","confidence":0.0}]}
                Rules:
                - Only user-stated or clearly confirmed information
                - Never infer medical diagnoses
                - Short reusable statements only
                - Return empty arrays if nothing durable
                - confidence is 0.0 to 1.0
                """);

            var response = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(
                        "You extract structured memory candidates from coaching chat. Output JSON only."),
                    new UserChatMessage(prompt.ToString())
                ],
                cancellationToken: cancellationToken);

            var json = response.Value.Content.FirstOrDefault()?.Text?.Trim() ?? "{}";
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                json = json[start..(end + 1)];
            }

            var parsed = JsonSerializer.Deserialize<MemoryExtractionJson>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return new MemoryExtractionResult
            {
                Facts = parsed?.Facts?.Select(f => new ExtractedFactCandidate
                {
                    Content = f.Content ?? string.Empty,
                    Category = f.Category ?? "general",
                    Confidence = f.Confidence
                }).Where(f => !string.IsNullOrWhiteSpace(f.Content)).ToList()
                    ?? new List<ExtractedFactCandidate>(),
                Episodes = parsed?.Episodes?.Select(e => new ExtractedEpisodeCandidate
                {
                    Summary = e.Summary ?? string.Empty,
                    Topic = e.Topic ?? "general",
                    Outcome = e.Outcome ?? "unknown",
                    Confidence = e.Confidence
                }).Where(e => !string.IsNullOrWhiteSpace(e.Summary)).ToList()
                    ?? new List<ExtractedEpisodeCandidate>()
            };
        }
        catch (Exception ex)
        {
            Services.Logger.LogError(ex, "Error extracting memory candidates");
            return new MemoryExtractionResult();
        }
    }

    private sealed class MemoryExtractionJson
    {
        public List<MemoryFactJson>? Facts { get; set; }
        public List<MemoryEpisodeJson>? Episodes { get; set; }
    }

    private sealed class MemoryFactJson
    {
        public string? Content { get; set; }
        public string? Category { get; set; }
        public double Confidence { get; set; }
    }

    private sealed class MemoryEpisodeJson
    {
        public string? Summary { get; set; }
        public string? Topic { get; set; }
        public string? Outcome { get; set; }
        public double Confidence { get; set; }
    }

    public async Task<(List<CitationInfo> Citations, string SourcesFormatted, Dictionary<int, string> CitationContents)> RetrieveCitationsAsync(
        string userQuery,
        string userId,
        ChatMemoryContext memoryContext,
        string? traceId = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = string.IsNullOrEmpty(traceId) ? "no-trace" : traceId;
        var sw = Stopwatch.StartNew();
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_retrieve_begin");

        AISettings aiSettings;
        try
        {
            aiSettings = await Services.ApplicationSettingsService.GetAISettingsAsync();
        }
        catch
        {
            aiSettings = new AISettings
            {
                MaxSearchResults = int.Parse(Services.Configuration["AI:MaxSearchResults"] ?? "5"),
                MaxContentLengthPerSource = int.Parse(Services.Configuration["AI:MaxContentLengthPerSource"] ?? "800"),
            };
        }

        if (memoryContext.Facts.Count > 0 || memoryContext.Episodes.Count > 0)
        {
            await Services.UserMemoryService.RecordRetrievalAsync(
                memoryContext.Facts.Select(f => f.Id).ToList(),
                memoryContext.Episodes.Select(e => e.Id).ToList());
        }

        var maxSearchResults = aiSettings.MaxSearchResults;
        var maxContentLengthPerSource = aiSettings.MaxContentLengthPerSource;

        var embedClient = Services.OpenAIClient.GetEmbeddingClient("openai-embed-deployment");
        var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
        var embedResponse = await embedClient.GenerateEmbeddingAsync(userQuery, embeddingOptions, cancellationToken);
        var queryVector = embedResponse.Value.ToFloats();
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_after_embedding");

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

        var searchResults = await Services.SearchIndexService.SearchAsync(userQuery, searchOptions);
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_after_search_async");

        var sourcesFormatted = new StringBuilder();
        var citations = new List<CitationInfo>();
        var citationContents = new Dictionary<int, string>();
        int citationIndex = 1;

        await foreach (var result in searchResults.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            var doc = result.Document;
            var id = doc.ContainsKey("id") ? doc["id"]?.ToString() ?? "" : "";
            var content = doc.ContainsKey("content") ? doc["content"]?.ToString() ?? "" : "";
            var sourceFile = doc.ContainsKey("sourcefile") ? doc["sourcefile"]?.ToString() ?? "Unknown" : "Unknown";
            var sourcePage = doc.ContainsKey("sourcepage") ? doc["sourcepage"]?.ToString() ?? "" : "";
            var storageUrl = doc.ContainsKey("storageUrl") ? doc["storageUrl"]?.ToString() ?? "" : "";
            var ownerId = doc.ContainsKey("ownerId") ? doc["ownerId"]?.ToString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var originalContent = content;

            if (content.Length > maxContentLengthPerSource)
            {
                content = content.Substring(0, maxContentLengthPerSource) + "... [truncated]";
            }

            citations.Add(new CitationInfo
            {
                Id = id,
                Index = citationIndex,
                SourceFile = sourceFile,
                SourcePage = sourcePage,
                StorageUrl = $"{storageUrl}",
                IsShared = string.IsNullOrEmpty(ownerId)
            });

            citationContents[citationIndex] = originalContent;

            sourcesFormatted.AppendLine($"[{citationIndex}] Source: {sourceFile}");
            if (!string.IsNullOrEmpty(sourcePage))
            {
                sourcesFormatted.AppendLine($"    Page/Section: {sourcePage}");
            }

            sourcesFormatted.AppendLine($"    Content: {content}");
            sourcesFormatted.AppendLine();

            citationIndex++;
        }

        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_retrieve_complete", citations.Count);
        return (citations, sourcesFormatted.ToString(), citationContents);
    }

    public async Task<(AsyncCollectionResult<StreamingChatCompletionUpdate> Response, List<CitationInfo> Citations)> GetResponseWithCitations(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<_shared.ChatMessage> history,
        ChatMemoryContext memoryContext,
        Guid conversationId,
        string? traceId = null)
    {
        return await getChatResponseWithCitations(
            userQuery, userId, userInformation, userName, history, memoryContext, conversationId, traceId);
    }

    public async Task<List<StrategySuggestion>> GetStrategySuggestionsAsync(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<_shared.ChatMessage> history,
        ChatMemoryContext memoryContext,
        Guid? conversationId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        Services.Logger.LogInformation("Generating strategy suggestions for user {UserId}", userId);

        var aiSettings = await Services.ApplicationSettingsService.GetAISettingsAsync();
        var promptBudgetSettings = await Services.ApplicationSettingsService.GetPromptBudgetSettingsAsync();
        var estimator = await TokenEstimatorFactory.CreateAsync(Services.ApplicationSettingsService);
        var includeOnboarding = ChatPromptAssembler.ShouldIncludeOnboardingBlob(
            aiSettings.IncludeFullOnboardingBlob,
            userInformation,
            memoryContext,
            isFirstTurn: history.Count == 0);

        var chatClient = Services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);
        var systemPrompt = ChatPromptAssembler.BuildStrategySystemPrompt(userName, memoryContext);

        List<_shared.ChatMessage> limitedHistory;
        string userPrompt;
        IReadOnlyList<PromptSlotTrace> slotTraces = Array.Empty<PromptSlotTrace>();

        if (promptBudgetSettings.EnablePromptBudget)
        {
            var initialHistory = ChatPromptAssembler.SelectRecentHistory(history, memoryContext).ToList();
            var budgeted = PromptTokenBudgetAssembler.AssembleForStrategy(
                userQuery,
                userInformation,
                memoryContext,
                initialHistory,
                promptBudgetSettings,
                aiSettings.MaxTotalTokens,
                estimator,
                includeOnboarding);
            userPrompt = budgeted.UserPrompt;
            limitedHistory = budgeted.LimitedHistory.ToList();
            slotTraces = budgeted.SlotTraces;
        }
        else
        {
            limitedHistory = ChatPromptAssembler.SelectRecentHistory(history, memoryContext).ToList();
            userPrompt = ChatPromptAssembler.BuildStrategyUserPrompt(
                userQuery,
                userInformation,
                memoryContext,
                limitedHistory,
                includeOnboarding);
        }

        var estimatedTokens = estimator.EstimateTokens(systemPrompt) + estimator.EstimateTokens(userPrompt);
        var traceEnvelope = ChatTurnTraceBuilder.Build(
            correlationId ?? "no-trace",
            conversationId ?? Guid.Empty,
            userId,
            memoryContext,
            limitedHistory.Count,
            citationCount: 0,
            citationIds: Array.Empty<string>(),
            retrievalMode: "strategies",
            estimatedTokens,
            aiSettings.MaxTotalTokens,
            truncatedSources: false,
            slotTraces);
        ChatTurnTraceBuilder.Log(Services.Logger, traceEnvelope, "StrategyTrace");

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
        ChatMemoryContext memoryContext,
        Guid conversationId,
        string? traceId)
    {
        var correlationId = string.IsNullOrEmpty(traceId) ? "no-trace" : traceId;
        var sw = Stopwatch.StartNew();
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_begin");

        Services.Logger.LogInformation("Starting getNewChatResponseWithCitations for userQuery: {UserQuery}, userId: {UserId}, userName: {UserName}", userQuery, userId, userName);

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
                MaxTotalTokens = int.Parse(Services.Configuration["AI:MaxTotalTokens"] ?? "7000"),
                RecentMessageWindow = int.Parse(Services.Configuration["AI:RecentMessageWindow"] ?? "4"),
                ConversationSummaryInterval = int.Parse(Services.Configuration["AI:ConversationSummaryInterval"] ?? "6"),
                MaxConversationSummaryLength = int.Parse(Services.Configuration["AI:MaxConversationSummaryLength"] ?? "1500"),
                EnableConversationSummary = !bool.TryParse(Services.Configuration["AI:EnableConversationSummary"], out var enabled) || enabled
            };
        }

        memoryContext = new ChatMemoryContext
        {
            Policies = memoryContext.Policies,
            UserPreferences = memoryContext.UserPreferences,
            ConversationSummary = memoryContext.ConversationSummary,
            Facts = memoryContext.Facts,
            Episodes = memoryContext.Episodes,
            MemoryRetrievalMode = memoryContext.MemoryRetrievalMode,
            RecentMessageWindow = memoryContext.RecentMessageWindow > 0
                ? memoryContext.RecentMessageWindow
                : aiSettings.RecentMessageWindow,
            MaxHistoryMessages = memoryContext.MaxHistoryMessages > 0
                ? memoryContext.MaxHistoryMessages
                : aiSettings.MaxHistoryMessages,
            EnableConversationSummary = memoryContext.EnableConversationSummary
        };

        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_after_get_ai_settings");

        var maxTotalTokens = aiSettings.MaxTotalTokens;

        var (citations, sourcesFormattedString, citationContents) = await RetrieveCitationsAsync(
            userQuery,
            userId,
            memoryContext,
            traceId,
            CancellationToken.None);

        var sourcesFormatted = new StringBuilder(sourcesFormattedString);
        var maxContentLengthPerSource = aiSettings.MaxContentLengthPerSource;

        // Build system prompt with instructions
        var systemPrompt = ChatPromptAssembler.BuildSystemPrompt(userName, citations.Count > 0, memoryContext);

        var promptBudgetSettings = await Services.ApplicationSettingsService.GetPromptBudgetSettingsAsync();
        var estimator = await TokenEstimatorFactory.CreateAsync(Services.ApplicationSettingsService);
        var includeOnboarding = ChatPromptAssembler.ShouldIncludeOnboardingBlob(
            aiSettings.IncludeFullOnboardingBlob,
            userInformation,
            memoryContext,
            isFirstTurn: history.Count == 0);
        List<_shared.ChatMessage> limitedHistory;
        string userPrompt;
        var truncatedSources = false;
        IReadOnlyList<PromptSlotTrace> slotTraces = Array.Empty<PromptSlotTrace>();

        if (promptBudgetSettings.EnablePromptBudget)
        {
            var initialHistory = ChatPromptAssembler.SelectRecentHistory(history, memoryContext).ToList();
            var budgeted = PromptTokenBudgetAssembler.Assemble(
                userQuery,
                userInformation,
                sourcesFormatted.ToString(),
                citations.Count,
                memoryContext,
                initialHistory,
                promptBudgetSettings,
                maxTotalTokens,
                estimator,
                includeOnboarding);
            userPrompt = budgeted.UserPrompt;
            limitedHistory = budgeted.LimitedHistory.ToList();
            truncatedSources = budgeted.TruncatedSources;
            slotTraces = budgeted.SlotTraces;
        }
        else
        {
            limitedHistory = ChatPromptAssembler.SelectRecentHistory(history, memoryContext).ToList();
            userPrompt = ChatPromptAssembler.BuildUserPrompt(
                userQuery, userInformation, sourcesFormatted.ToString(), citations.Count, memoryContext, includeOnboarding);
        }

        Services.Logger.LogInformation("Formatted prompt with {SourceCount} sources and {HistoryCount} history messages (limited from {OriginalHistoryCount})",
            citations.Count, limitedHistory.Count, history.Count);

        // Create chat messages using limited history
        var messages = ToChatMessages(limitedHistory);

        // Combine system prompt with user prompt and add as final user message
        var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

        // Estimate tokens (rough approximation: 1 token ≈ 4 characters)
        var estimatedTokens = estimator.EstimateTokens(combinedPrompt);

        // Also estimate tokens from history messages
        foreach (var msg in messages)
        {
            estimatedTokens += estimator.EstimateTokens(msg.Content.ToString());
        }

        Services.Logger.LogInformation("Sending {MessageCount} messages to ChatClient", messages.Count);
        Services.Logger.LogInformation("Prompt Length: {PromptLength}", combinedPrompt.Length);
        Services.Logger.LogInformation("Estimated Total Tokens: {Tokens}", estimatedTokens);

        // If still over limit without budget assembler, truncate sources further
        if (!promptBudgetSettings.EnablePromptBudget && estimatedTokens > maxTotalTokens)
        {
            truncatedSources = true;
            Services.Logger.LogWarning("Estimated tokens ({EstimatedTokens}) exceed limit ({MaxTokens}). Truncating sources further.",
                estimatedTokens, maxTotalTokens);

            // Reduce content length per source
            var reductionFactor = (double)maxTotalTokens / estimatedTokens;
            var newMaxContentLength = (int)(maxContentLengthPerSource * reductionFactor * 0.9); // 90% to be safe

            sourcesFormatted.Clear();
            int citationIndex = 1;

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
            userPrompt = ChatPromptAssembler.BuildUserPrompt(
                userQuery, userInformation, sourcesFormatted.ToString(), citations.Count, memoryContext, includeOnboarding);
            combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

            estimatedTokens = estimator.EstimateTokens(combinedPrompt)
                + messages.Sum(m => estimator.EstimateTokens(m.Content.ToString()));
            Services.Logger.LogInformation("After truncation - Estimated Total Tokens: {Tokens}", estimatedTokens);
        }

        messages.Add(new UserChatMessage(combinedPrompt));

        var traceEnvelope = ChatTurnTraceBuilder.Build(
            correlationId,
            conversationId,
            userId,
            memoryContext,
            limitedHistory.Count,
            citations.Count,
            citations.Select(c => c.Id).ToList(),
            "vector",
            estimatedTokens,
            maxTotalTokens,
            truncatedSources,
            slotTraces);
        ChatTurnTraceBuilder.Log(Services.Logger, traceEnvelope);

        // Get streaming response
        var chatClient = Services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_before_complete_chat_streaming", messages.Count);
        var response = chatClient.CompleteChatStreamingAsync(messages);
        LogChatRagPhase(Services.Logger, correlationId, sw, "rag_returning_stream");

        return (response, citations);
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

        builder.Services.AddSingleton<ITokenEstimator, CharacterTokenEstimator>();

        // Register AgentOpenAIService (implements IAgentOpenAIService from Domain)
        builder.Services.AddScoped<ProjectBrain.Domain.IAgentOpenAIService>(sp =>
        {
            var services = new AzureOpenAIServices(
                sp.GetRequiredService<OpenAIClient>(),
                sp.GetRequiredService<ISearchIndexService>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IApplicationSettingsService>(),
                sp.GetRequiredService<IUserMemoryService>(),
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
        builder.Services.AddScoped<ProjectBrain.Domain.IUserMemoryIndexService, UserMemoryIndexService>();
        builder.Services.AddScoped<ProjectBrain.Domain.IUserMemoryRetrievalService, UserMemoryRetrievalService>();
        builder.Services.AddScoped<ProjectBrain.Domain.IUserBlobErasureService, UserBlobErasureService>();
        builder.Services.AddScoped<ProjectBrain.Domain.IUserSearchIndexErasureService, UserSearchIndexErasureService>();
    }
}
