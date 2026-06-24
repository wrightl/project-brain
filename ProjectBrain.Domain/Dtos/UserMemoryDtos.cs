namespace ProjectBrain.Domain.Dtos;

public sealed class RetrievedUserFact
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public string Category { get; init; } = "general";
}

public sealed class RetrievedUserEpisode
{
    public required Guid Id { get; init; }
    public required string Summary { get; init; }
    public string Topic { get; init; } = "general";
    public string Outcome { get; init; } = "unknown";
}

public sealed class UserFactDto
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public string Category { get; init; } = "general";
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsPinned { get; init; }
}

public sealed class UserEpisodeDto
{
    public required Guid Id { get; init; }
    public required string Summary { get; init; }
    public string Topic { get; init; } = "general";
    public string Outcome { get; init; } = "unknown";
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsPinned { get; init; }
}

public sealed class UserMemoryListDto
{
    public IReadOnlyList<UserFactDto> Facts { get; init; } = Array.Empty<UserFactDto>();
    public IReadOnlyList<UserEpisodeDto> Episodes { get; init; } = Array.Empty<UserEpisodeDto>();
}

public sealed class MemoryExtractionResult
{
    public IReadOnlyList<ExtractedFactCandidate> Facts { get; init; } = Array.Empty<ExtractedFactCandidate>();
    public IReadOnlyList<ExtractedEpisodeCandidate> Episodes { get; init; } = Array.Empty<ExtractedEpisodeCandidate>();
}

public sealed class ExtractedFactCandidate
{
    public required string Content { get; init; }
    public string Category { get; init; } = "general";
    public double Confidence { get; init; }
}

public sealed class ExtractedEpisodeCandidate
{
    public required string Summary { get; init; }
    public string Topic { get; init; } = "general";
    public string Outcome { get; init; } = "unknown";
    public double Confidence { get; init; }
}

public sealed class MemoryRetrievalResult
{
    public IReadOnlyList<RetrievedUserFact> Facts { get; init; } = Array.Empty<RetrievedUserFact>();
    public IReadOnlyList<RetrievedUserEpisode> Episodes { get; init; } = Array.Empty<RetrievedUserEpisode>();
    public string RetrievalMode { get; init; } = "disabled";
}

public sealed class MemorySettings
{
    public bool EnableMemoryFormation { get; set; } = true;
    public double MinPromotionConfidence { get; set; } = 0.75;
    public double ProvisionalConfidence { get; set; } = 0.60;
    public int ActivationObservationCount { get; set; } = 2;
    public int MaxFactsPerTurn { get; set; } = 3;
    public int MaxEpisodesPerTurn { get; set; } = 2;
    public int MaxFactsRetrieved { get; set; } = 5;
    public int MaxEpisodesRetrieved { get; set; } = 3;
    public bool IndexProvisionalMemories { get; set; }
    public bool EnableMemoryDecay { get; set; } = true;
    public int ProvisionalTtlDays { get; set; } = 30;
    public int ActiveFactTtlDays { get; set; } = 365;
    public int ActiveEpisodeTtlDays { get; set; } = 180;
    public int DecayInactivityDays { get; set; } = 90;
}
