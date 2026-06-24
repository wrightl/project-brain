namespace ProjectBrain.Shared.Dtos.Settings;

public class PromptBudgetSettingsDto
{
    public required bool EnablePromptBudget { get; init; }
    public required int SystemReserve { get; init; }
    public required int PoliciesReserve { get; init; }
    public required int PreferencesReserve { get; init; }
    public required int QueryReserve { get; init; }
    public required int SummaryReserve { get; init; }
    public required int FactsReserve { get; init; }
    public required int EpisodesReserve { get; init; }
    public required int OnboardingReserve { get; init; }
    public required int HistoryReserve { get; init; }
    public required int ToolDefinitionsReserve { get; init; }
    public required int ToolResultsReserve { get; init; }
    public required string TokenEstimator { get; init; }
}

public class UpdatePromptBudgetSettingsRequestDto
{
    public required bool EnablePromptBudget { get; init; }
    public required int SystemReserve { get; init; }
    public required int PoliciesReserve { get; init; }
    public required int PreferencesReserve { get; init; }
    public required int QueryReserve { get; init; }
    public required int SummaryReserve { get; init; }
    public required int FactsReserve { get; init; }
    public required int EpisodesReserve { get; init; }
    public required int OnboardingReserve { get; init; }
    public required int HistoryReserve { get; init; }
    public required int ToolDefinitionsReserve { get; init; }
    public required int ToolResultsReserve { get; init; }
    public required string TokenEstimator { get; init; }
}
