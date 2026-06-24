using ProjectBrain.AI;
using ProjectBrain.Api.Background;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace ProjectBrain.Api.Services;

public sealed class JournalAgentService : IJournalAgentService
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly AzureOpenAI _azureOpenAI;
    private readonly ITimeTickerManager<TimeTickerEntity> _timeTickerManager;

    public JournalAgentService(
        IJournalEntryService journalEntryService,
        AzureOpenAI azureOpenAI,
        ITimeTickerManager<TimeTickerEntity> timeTickerManager)
    {
        _journalEntryService = journalEntryService;
        _azureOpenAI = azureOpenAI;
        _timeTickerManager = timeTickerManager;
    }

    public async Task<JournalAgentEntryResult> CreateEntryAsync(
        string userId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var summary = await _azureOpenAI.GetConversationSummary(content, userId);

        var journalEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = content,
            Summary = summary,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdEntry = await _journalEntryService.Add(journalEntry);
        await UserContextTickerEnqueue.EnqueueJournalUploadAsync(_timeTickerManager, userId, createdEntry.Id, cancellationToken);

        return new JournalAgentEntryResult
        {
            Id = createdEntry.Id,
            Summary = createdEntry.Summary,
            CreatedAt = createdEntry.CreatedAt
        };
    }
}
