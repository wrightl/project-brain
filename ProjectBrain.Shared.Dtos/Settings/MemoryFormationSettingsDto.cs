namespace ProjectBrain.Shared.Dtos.Settings;

/// <summary>DTO for memory formation settings.</summary>
public class MemoryFormationSettingsDto
{
    public required bool EnableMemoryFormation { get; init; }
    public required double MinPromotionConfidence { get; init; }
    public required double ProvisionalConfidence { get; init; }
    public required int ActivationObservationCount { get; init; }
    public required int MaxFactsPerTurn { get; init; }
    public required int MaxEpisodesPerTurn { get; init; }
    public required int MaxFactsRetrieved { get; init; }
    public required int MaxEpisodesRetrieved { get; init; }
    public required bool IndexProvisionalMemories { get; init; }
    public required bool EnableMemoryDecay { get; init; }
    public required int ProvisionalTtlDays { get; init; }
    public required int ActiveFactTtlDays { get; init; }
    public required int ActiveEpisodeTtlDays { get; init; }
    public required int DecayInactivityDays { get; init; }
}

/// <summary>Request DTO for updating memory formation settings.</summary>
public class UpdateMemoryFormationSettingsRequestDto
{
    public required bool EnableMemoryFormation { get; init; }
    public required double MinPromotionConfidence { get; init; }
    public required double ProvisionalConfidence { get; init; }
    public required int ActivationObservationCount { get; init; }
    public required int MaxFactsPerTurn { get; init; }
    public required int MaxEpisodesPerTurn { get; init; }
    public required int MaxFactsRetrieved { get; init; }
    public required int MaxEpisodesRetrieved { get; init; }
    public required bool IndexProvisionalMemories { get; init; }
    public required bool EnableMemoryDecay { get; init; }
    public required int ProvisionalTtlDays { get; init; }
    public required int ActiveFactTtlDays { get; init; }
    public required int ActiveEpisodeTtlDays { get; init; }
    public required int DecayInactivityDays { get; init; }
}
