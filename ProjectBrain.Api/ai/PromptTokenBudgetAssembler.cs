namespace ProjectBrain.AI;

using ProjectBrain.Domain.Dtos;
using _shared = Models;

public interface ITokenEstimator
{
    int EstimateTokens(string? text);
}

public sealed class CharacterTokenEstimator : ITokenEstimator
{
    public int EstimateTokens(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return text.Length / 4;
    }
}

public sealed class BudgetedPromptResult
{
    public required string UserPrompt { get; init; }
    public IReadOnlyList<PromptSlotTrace> SlotTraces { get; init; } = Array.Empty<PromptSlotTrace>();
    public IReadOnlyList<_shared.ChatMessage> LimitedHistory { get; init; } = Array.Empty<_shared.ChatMessage>();
    public bool TruncatedSources { get; init; }
}

public static class PromptTokenBudgetAssembler
{
    public static BudgetedPromptResult Assemble(
        string userQuery,
        string userInformation,
        string sourcesFormatted,
        int citationCount,
        ChatMemoryContext memoryContext,
        IReadOnlyList<_shared.ChatMessage> history,
        PromptBudgetSettings budget,
        int maxTotalTokens,
        ITokenEstimator estimator)
    {
        var slotTraces = new List<PromptSlotTrace>();
        var reserved = budget.SystemReserve + budget.PoliciesReserve + budget.PreferencesReserve
            + budget.QueryReserve;
        var remaining = Math.Max(0, maxTotalTokens - reserved);

        var summaryText = TrimSummary(memoryContext.ConversationSummary, budget.SummaryReserve, remaining, estimator, slotTraces);
        remaining -= slotTraces.Last().EstimatedTokens;

        var trimmedFacts = TrimFactList(memoryContext.Facts, budget.FactsReserve, remaining, estimator, slotTraces);
        remaining -= slotTraces.Last().EstimatedTokens;

        var trimmedEpisodes = TrimEpisodeList(memoryContext.Episodes, budget.EpisodesReserve, remaining, estimator, slotTraces);
        remaining -= slotTraces.Last().EstimatedTokens;

        var onboardingBudget = Math.Min(budget.OnboardingReserve, remaining);
        var trimmedOnboarding = TrimToCharBudget(userInformation, onboardingBudget, estimator);
        slotTraces.Add(new PromptSlotTrace
        {
            SlotName = "onboarding",
            EstimatedTokens = estimator.EstimateTokens(trimmedOnboarding),
            Truncated = trimmedOnboarding.Length < (userInformation?.Length ?? 0)
        });
        remaining -= slotTraces.Last().EstimatedTokens;

        var sourcesBudget = Math.Max(0, remaining - budget.HistoryReserve);
        var trimmedSources = TrimToCharBudget(sourcesFormatted, sourcesBudget, estimator);
        var truncatedSources = trimmedSources.Length < sourcesFormatted.Length;
        slotTraces.Add(new PromptSlotTrace
        {
            SlotName = "sources",
            EstimatedTokens = estimator.EstimateTokens(trimmedSources),
            Truncated = truncatedSources
        });
        remaining -= slotTraces.Last().EstimatedTokens;

        var historyBudget = Math.Min(budget.HistoryReserve, remaining);
        var limitedHistory = SelectHistoryByTokenBudget(history, historyBudget, estimator, slotTraces);

        var trimmedContext = new ChatMemoryContext
        {
            Policies = memoryContext.Policies,
            UserPreferences = memoryContext.UserPreferences,
            ConversationSummary = summaryText,
            Facts = trimmedFacts,
            Episodes = trimmedEpisodes,
            MemoryRetrievalMode = memoryContext.MemoryRetrievalMode,
            RecentMessageWindow = memoryContext.RecentMessageWindow,
            MaxHistoryMessages = memoryContext.MaxHistoryMessages,
            EnableConversationSummary = memoryContext.EnableConversationSummary
        };

        var userPrompt = ChatPromptAssembler.BuildUserPrompt(
            userQuery,
            trimmedOnboarding,
            trimmedSources,
            citationCount,
            trimmedContext);

        slotTraces.Add(new PromptSlotTrace
        {
            SlotName = "query",
            EstimatedTokens = estimator.EstimateTokens(userQuery),
            Truncated = false
        });

        return new BudgetedPromptResult
        {
            UserPrompt = userPrompt,
            SlotTraces = slotTraces,
            LimitedHistory = limitedHistory,
            TruncatedSources = truncatedSources
        };
    }

    private static string? TrimSummary(
        string? summary,
        int tokenBudget,
        int remainingBudget,
        ITokenEstimator estimator,
        List<PromptSlotTrace> slotTraces)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            slotTraces.Add(new PromptSlotTrace { SlotName = "summary", EstimatedTokens = 0 });
            return null;
        }

        var effectiveBudget = Math.Min(tokenBudget, remainingBudget);
        var maxChars = effectiveBudget * 4;
        var trimmed = summary.Length <= maxChars ? summary : summary[..maxChars];
        slotTraces.Add(new PromptSlotTrace
        {
            SlotName = "summary",
            EstimatedTokens = estimator.EstimateTokens(trimmed),
            Truncated = trimmed.Length < summary.Length
        });
        return trimmed;
    }

    private static IReadOnlyList<RetrievedUserFact> TrimFactList(
        IReadOnlyList<RetrievedUserFact> facts,
        int tokenBudget,
        int remainingBudget,
        ITokenEstimator estimator,
        List<PromptSlotTrace> slotTraces)
    {
        if (facts.Count == 0)
        {
            slotTraces.Add(new PromptSlotTrace { SlotName = "facts", EstimatedTokens = 0 });
            return facts;
        }

        var effectiveBudget = Math.Min(tokenBudget, remainingBudget);
        var kept = new List<RetrievedUserFact>();
        var sb = new System.Text.StringBuilder("## What I know about you\n");
        foreach (var fact in facts)
        {
            var line = $"- {fact.Content}\n";
            if (estimator.EstimateTokens(sb + line) > effectiveBudget)
            {
                break;
            }

            sb.Append(line);
            kept.Add(fact);
        }

        slotTraces.Add(new PromptSlotTrace
        {
            SlotName = "facts",
            EstimatedTokens = estimator.EstimateTokens(sb.ToString()),
            DroppedCount = facts.Count - kept.Count,
            Truncated = kept.Count < facts.Count
        });
        return kept;
    }

    private static IReadOnlyList<RetrievedUserEpisode> TrimEpisodeList(
        IReadOnlyList<RetrievedUserEpisode> episodes,
        int tokenBudget,
        int remainingBudget,
        ITokenEstimator estimator,
        List<PromptSlotTrace> slotTraces)
    {
        if (episodes.Count == 0)
        {
            slotTraces.Add(new PromptSlotTrace { SlotName = "episodes", EstimatedTokens = 0 });
            return episodes;
        }

        var effectiveBudget = Math.Min(tokenBudget, remainingBudget);
        var kept = new List<RetrievedUserEpisode>();
        var sb = new System.Text.StringBuilder("## Past experiences that may help\n");
        foreach (var episode in episodes)
        {
            var line = $"- {episode.Summary} (topic: {episode.Topic}, outcome: {episode.Outcome})\n";
            if (estimator.EstimateTokens(sb + line) > effectiveBudget)
            {
                break;
            }

            sb.Append(line);
            kept.Add(episode);
        }

        slotTraces.Add(new PromptSlotTrace
        {
            SlotName = "episodes",
            EstimatedTokens = estimator.EstimateTokens(sb.ToString()),
            DroppedCount = episodes.Count - kept.Count,
            Truncated = kept.Count < episodes.Count
        });
        return kept;
    }

    private static string TrimToCharBudget(string? text, int tokenBudget, ITokenEstimator estimator)
    {
        if (string.IsNullOrEmpty(text) || tokenBudget <= 0)
        {
            return string.Empty;
        }

        var maxChars = tokenBudget * 4;
        return text.Length <= maxChars ? text : text[..maxChars];
    }

    private static IReadOnlyList<_shared.ChatMessage> SelectHistoryByTokenBudget(
        IReadOnlyList<_shared.ChatMessage> history,
        int tokenBudget,
        ITokenEstimator estimator,
        List<PromptSlotTrace> slotTraces)
    {
        if (history.Count == 0 || tokenBudget <= 0)
        {
            slotTraces.Add(new PromptSlotTrace { SlotName = "history", EstimatedTokens = 0 });
            return history;
        }

        var selected = new List<_shared.ChatMessage>();
        var used = 0;
        foreach (var message in history.AsEnumerable().Reverse())
        {
            var cost = estimator.EstimateTokens(message.Content);
            if (used + cost > tokenBudget)
            {
                break;
            }

            selected.Insert(0, message);
            used += cost;
        }

        slotTraces.Add(new PromptSlotTrace
        {
            SlotName = "history",
            EstimatedTokens = used,
            DroppedCount = history.Count - selected.Count,
            Truncated = selected.Count < history.Count
        });
        return selected;
    }
}
