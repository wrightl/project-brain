namespace ProjectBrain.Domain.Dtos;

public sealed class PromptBudgetSettings
{
    public bool EnablePromptBudget { get; set; }
    public int SystemReserve { get; set; } = 400;
    public int PoliciesReserve { get; set; } = 300;
    public int PreferencesReserve { get; set; } = 200;
    public int QueryReserve { get; set; } = 200;
    public int SummaryReserve { get; set; } = 400;
    public int FactsReserve { get; set; } = 300;
    public int EpisodesReserve { get; set; } = 300;
    public int OnboardingReserve { get; set; } = 500;
    public int HistoryReserve { get; set; } = 800;
}

public sealed class PromptSlotTrace
{
    public required string SlotName { get; init; }
    public int EstimatedTokens { get; init; }
    public int DroppedCount { get; init; }
    public bool Truncated { get; init; }
}
