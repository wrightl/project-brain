using System.Text;
using System.Linq;
using ProjectBrain.AI;
using ProjectBrain.Domain;
using ProjectBrain.Database.Models;
using TickerQ.Utilities.Base;

namespace ProjectBrain.Api.Background;

/// <summary>TickerQ job functions for user-context markdown upload and indexing.</summary>
public class UserContextTickerJobs(IServiceScopeFactory serviceScopeFactory, ILogger<UserContextTickerJobs> logger)
{
    [TickerFunction("UserContext_JournalUpload")]
    public async Task JournalUpload(
        TickerFunctionContext<JournalUploadRequest> context,
        CancellationToken cancellationToken)
    {
        var req = context.Request;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var azureOpenAI = scope.ServiceProvider.GetRequiredService<AzureOpenAI>();
            var storage = scope.ServiceProvider.GetRequiredService<Storage>();
            var journalEntryService = scope.ServiceProvider.GetRequiredService<IJournalEntryService>();

            var entry = await journalEntryService.GetById(req.EntryId, req.UserId);
            if (entry == null)
            {
                logger.LogWarning("Journal entry {EntryId} not found for upload", req.EntryId);
                return;
            }

            // var summary = await azureOpenAI.GetConversationSummary(entry.Content, req.UserId);
            // entry.Summary = summary;
            // entry.UpdatedAt = DateTime.UtcNow;
            // await journalEntryService.Update(entry, null, null);

            var markdown = BuildJournalEntryMarkdown(entry.Content, entry.Summary, entry.CreatedAt);
            var mdFilename = $"{req.EntryId}.md";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
            var options = new StorageUploadOptions
            {
                UserId = req.UserId,
                StorageType = StorageType.Journal,
                ResourceId = req.EntryId.ToString(),
                SkipIndexing = false
            };
            await storage.UploadFile(stream, mdFilename, options);
            logger.LogInformation("Processed journal entry {EntryId}", req.EntryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing journal entry {EntryId}", req.EntryId);
            throw;
        }
    }

    [TickerFunction("UserContext_JournalDelete")]
    public async Task JournalDelete(
        TickerFunctionContext<JournalDeleteRequest> context,
        CancellationToken cancellationToken)
    {
        var req = context.Request;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<Storage>();
            var options = new StorageOptions
            {
                UserId = req.UserId,
                StorageType = StorageType.Journal,
            };
            await storage.DeleteFile($"{req.EntryId}.md", options);
            logger.LogInformation("Deleted journal entry blob and index {EntryId}", req.EntryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting journal entry {EntryId}", req.EntryId);
            throw;
        }
    }

    [TickerFunction("UserContext_GoalsUpload")]
    public async Task GoalsUpload(
        TickerFunctionContext<GoalsUploadRequest> context,
        CancellationToken cancellationToken)
    {
        var req = context.Request;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var goalService = scope.ServiceProvider.GetRequiredService<IGoalService>();
            var storage = scope.ServiceProvider.GetRequiredService<Storage>();
            var searchIndexService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();

            var goals = await goalService.GetTodaysGoalsAsync(req.UserId, cancellationToken);
            var markdown = BuildGoalsMarkdown(goals);
            var options = new StorageOptions { UserId = req.UserId, FileOwnership = FileOwnership.User, StorageType = StorageType.Goals };
            var blobPath = storage.determineLocation(Constants.GOALS_FILENAME, options);
            await searchIndexService.DeleteDocumentsFromIndexAsync(Constants.GOALS_FILENAME, blobPath);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
            var uploadOptions = new StorageUploadOptions
            {
                UserId = req.UserId,
                FileOwnership = FileOwnership.User,
                StorageType = StorageType.Goals,
                ResourceId = "goals",
                SkipIndexing = false
            };
            await storage.UploadFile(stream, Constants.GOALS_FILENAME, uploadOptions);
            logger.LogInformation("Uploaded goals markdown for user {UserId}", req.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading goals markdown for user {UserId}", req.UserId);
            throw;
        }
    }

    [TickerFunction("UserContext_StrategyUpload")]
    public async Task StrategyUpload(
        TickerFunctionContext<StrategyUploadRequest> context,
        CancellationToken cancellationToken)
    {
        var req = context.Request;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<Storage>();
            var searchIndexService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();

            var markdown = BuildStrategyMarkdown(req.Title, req.Description, req.IconKey, req.Rating, req.SavedAt);
            var filename = $"{req.StrategyId}.md";
            var options = new StorageOptions { UserId = req.UserId, FileOwnership = FileOwnership.User, StorageType = StorageType.Strategies };
            var blobPath = storage.determineLocation(filename, options);
            await searchIndexService.DeleteDocumentsFromIndexAsync(filename, blobPath);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
            var uploadOptions = new StorageUploadOptions
            {
                UserId = req.UserId,
                FileOwnership = FileOwnership.User,
                StorageType = StorageType.Strategies,
                ResourceId = req.StrategyId.ToString(),
                SkipIndexing = false
            };
            await storage.UploadFile(stream, filename, uploadOptions);
            logger.LogInformation("Uploaded coping strategy markdown {StrategyId}", req.StrategyId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading coping strategy markdown {StrategyId}", req.StrategyId);
            throw;
        }
    }

    [TickerFunction("UserContext_VoiceNoteTranscribe")]
    public async Task VoiceNoteTranscribe(
        TickerFunctionContext<VoiceNoteTranscribeRequest> context,
        CancellationToken cancellationToken)
    {
        var req = context.Request;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var azureOpenAI = scope.ServiceProvider.GetRequiredService<AzureOpenAI>();
            var storage = scope.ServiceProvider.GetRequiredService<Storage>();

            var options = new StorageOptions { UserId = req.UserId, FileOwnership = FileOwnership.User, StorageType = StorageType.VoiceNotes };
            var audioStream = await storage.GetFile(req.AudioBlobName, options);
            if (audioStream == null)
            {
                logger.LogWarning("Audio blob not found for voice note {VoiceNoteId}", req.VoiceNoteId);
                return;
            }

            var extension = Path.GetExtension(req.AudioBlobName);
            var transcribedText = await azureOpenAI.TranscribeAudio(audioStream, $"voice_note{extension}");
            await audioStream.DisposeAsync();

            var markdown = BuildVoiceNoteTranscriptMarkdown(transcribedText, DateTime.UtcNow);
            var transcriptFilename = $"{req.VoiceNoteId}-transcript.md";
            using var mdStream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
            var uploadOptions = new StorageUploadOptions
            {
                UserId = req.UserId,
                FileOwnership = FileOwnership.User,
                StorageType = StorageType.VoiceNotes,
                ResourceId = req.VoiceNoteId.ToString(),
                SkipIndexing = false
            };
            await storage.UploadFile(mdStream, transcriptFilename, uploadOptions);
            logger.LogInformation("Transcribed and indexed voice note {VoiceNoteId}", req.VoiceNoteId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error transcribing voice note {VoiceNoteId}", req.VoiceNoteId);
            throw;
        }
    }

    [TickerFunction("UserContext_ConversationTitleSummary")]
    public async Task ConversationTitleSummary(
        TickerFunctionContext<ConversationTitleSummaryRequest> context,
        CancellationToken cancellationToken)
    {
        var req = context.Request;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
            var azureOpenAI = scope.ServiceProvider.GetRequiredService<AzureOpenAI>();

            var conversation = await conversationService.GetById(req.ConversationId, req.UserId);
            if (conversation is null)
            {
                logger.LogWarning("Conversation {ConversationId} not found for title summary; skipping", req.ConversationId);
                return;
            }

            var summary = await azureOpenAI.GetConversationSummary(req.UserMessageContent, req.UserId);
            if (string.IsNullOrWhiteSpace(summary))
            {
                logger.LogWarning("Empty summary for conversation {ConversationId}; leaving title unchanged", req.ConversationId);
                return;
            }

            const int maxTitleLen = 128;
            conversation.Title = summary.Length > maxTitleLen ? summary[..maxTitleLen] : summary;
            conversation.UpdatedAt = DateTime.UtcNow;
            await conversationService.Update(conversation);
            logger.LogInformation("Updated conversation {ConversationId} title from deferred summary", req.ConversationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating conversation title for {ConversationId}", req.ConversationId);
        }
    }

    [TickerFunction("UserContext_ConversationContextSummary")]
    public async Task ConversationContextSummary(
        TickerFunctionContext<ConversationContextSummaryRequest> context,
        CancellationToken cancellationToken)
    {
        var req = context.Request;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
            var azureOpenAI = scope.ServiceProvider.GetRequiredService<AzureOpenAI>();
            var applicationSettingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();

            var aiSettings = await applicationSettingsService.GetAISettingsAsync();
            if (!aiSettings.EnableConversationSummary)
            {
                logger.LogDebug("Conversation context summary disabled; skipping {ConversationId}", req.ConversationId);
                return;
            }

            var conversation = await conversationService.GetByIdWithMessages(req.ConversationId, req.UserId);
            if (conversation is null)
            {
                logger.LogWarning("Conversation {ConversationId} not found for context summary; skipping", req.ConversationId);
                return;
            }

            var messageCount = conversation.Messages.Count;
            var newMessageCount = messageCount - conversation.SummaryMessageCount;
            var hasSummary = !string.IsNullOrWhiteSpace(conversation.ContextSummary);

            if (hasSummary && newMessageCount < aiSettings.ConversationSummaryInterval)
            {
                logger.LogDebug(
                    "Skipping context summary for {ConversationId}: only {NewCount} new messages (interval {Interval})",
                    req.ConversationId,
                    newMessageCount,
                    aiSettings.ConversationSummaryInterval);
                return;
            }

            if (messageCount == 0)
            {
                return;
            }

            var newMessages = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Skip(conversation.SummaryMessageCount)
                .ToList();

            if (newMessages.Count == 0 && hasSummary)
            {
                return;
            }

            var messagesToSummarize = newMessages.Count > 0
                ? newMessages
                : conversation.Messages.OrderBy(m => m.CreatedAt).ToList();

            var summary = await azureOpenAI.UpdateConversationContextSummaryAsync(
                conversation.ContextSummary,
                messagesToSummarize,
                aiSettings.MaxConversationSummaryLength,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(summary))
            {
                logger.LogWarning("Empty context summary for conversation {ConversationId}; leaving unchanged", req.ConversationId);
                return;
            }

            conversation.ContextSummary = summary;
            conversation.SummaryMessageCount = messageCount;
            conversation.UpdatedAt = DateTime.UtcNow;
            await conversationService.Update(conversation);
            logger.LogInformation(
                "Updated conversation {ConversationId} context summary ({Length} chars, {MessageCount} messages)",
                req.ConversationId,
                summary.Length,
                messageCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating conversation context summary for {ConversationId}", req.ConversationId);
        }
    }

    [TickerFunction("UserContext_MemoryExtraction")]
    public async Task MemoryExtraction(
        TickerFunctionContext<MemoryExtractionRequest> context,
        CancellationToken cancellationToken)
    {
        var req = context.Request;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var applicationSettingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();
            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
            var azureOpenAI = scope.ServiceProvider.GetRequiredService<AzureOpenAI>();
            var memoryPromotionService = scope.ServiceProvider.GetRequiredService<IMemoryPromotionService>();

            var memorySettings = await applicationSettingsService.GetMemorySettingsAsync(cancellationToken);
            if (!memorySettings.EnableMemoryFormation)
            {
                logger.LogDebug("Memory formation disabled; skipping extraction for {ConversationId}", req.ConversationId);
                return;
            }

            var conversation = await conversationService.GetById(req.ConversationId, req.UserId);
            var extraction = await azureOpenAI.ExtractMemoryCandidatesAsync(
                req.UserContent,
                req.AssistantContent,
                conversation?.ContextSummary,
                cancellationToken);

            await memoryPromotionService.ProcessExtractionAsync(
                req.UserId,
                req.ConversationId,
                extraction,
                memorySettings,
                cancellationToken);

            logger.LogInformation(
                "Processed memory extraction for conversation {ConversationId}: {FactCount} facts, {EpisodeCount} episodes",
                req.ConversationId,
                extraction.Facts.Count,
                extraction.Episodes.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing memory extraction for {ConversationId}", req.ConversationId);
        }
    }

    private static string BuildJournalEntryMarkdown(string content, string? summary, DateTime createdAt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Journal entry");
        sb.AppendLine();
        sb.AppendLine("## Date");
        sb.AppendLine(createdAt.ToString("O"));
        sb.AppendLine();
        sb.AppendLine("## Content");
        sb.AppendLine(content);
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(summary))
        {
            sb.AppendLine("## Summary");
            sb.AppendLine(summary);
        }
        return sb.ToString();
    }

    private static string BuildGoalsMarkdown(IEnumerable<Goal> goals)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Goals");
        sb.AppendLine();
        sb.AppendLine("## Today's goals");
        sb.AppendLine();
        var list = goals.OrderBy(g => g.Index).ToList();
        for (int i = 0; i < list.Count; i++)
        {
            var g = list[i];
            sb.AppendLine($"### Goal {i + 1}");
            sb.AppendLine(string.IsNullOrWhiteSpace(g.Message) ? "(empty)" : g.Message);
            sb.AppendLine("- **Completed:** " + (g.Completed ? "Yes" : "No"));
            if (g.CompletedAt.HasValue)
                sb.AppendLine("- **Completed at:** " + g.CompletedAt.Value.ToString("O"));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string BuildStrategyMarkdown(string title, string description, string? iconKey, int? rating, DateTime savedAt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Coping strategy");
        sb.AppendLine();
        sb.AppendLine("## Title");
        sb.AppendLine(title);
        sb.AppendLine();
        sb.AppendLine("## Description");
        sb.AppendLine(description);
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(iconKey))
        {
            sb.AppendLine("## Icon");
            sb.AppendLine(iconKey);
            sb.AppendLine();
        }
        if (rating.HasValue)
        {
            sb.AppendLine("## Rating");
            sb.AppendLine(rating.Value.ToString());
            sb.AppendLine();
        }
        sb.AppendLine("## Saved at");
        sb.AppendLine(savedAt.ToString("O"));
        return sb.ToString();
    }

    private static string BuildVoiceNoteTranscriptMarkdown(string transcript, DateTime createdAt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Voice note transcript");
        sb.AppendLine();
        sb.AppendLine("## Date");
        sb.AppendLine(createdAt.ToString("O"));
        sb.AppendLine();
        sb.AppendLine("## Transcript");
        sb.AppendLine(string.IsNullOrWhiteSpace(transcript) ? "(No speech detected)" : transcript);
        return sb.ToString();
    }
}
