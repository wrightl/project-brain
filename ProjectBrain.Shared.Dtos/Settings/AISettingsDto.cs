namespace ProjectBrain.Shared.Dtos.Settings;

/// <summary>
/// DTO for AI settings
/// </summary>
public class AISettingsDto
{
    public required int MaxSearchResults { get; init; }
    public required int MaxContentLengthPerSource { get; init; }
    public required int MaxHistoryMessages { get; init; }
    public required int MaxTotalTokens { get; init; }
    public required bool IncludeFullOnboardingBlob { get; init; }
}

/// <summary>
/// DTO for updating AI settings
/// </summary>
public class UpdateAISettingsRequestDto
{
    public required int MaxSearchResults { get; init; }
    public required int MaxContentLengthPerSource { get; init; }
    public required int MaxHistoryMessages { get; init; }
    public required int MaxTotalTokens { get; init; }
    public required bool IncludeFullOnboardingBlob { get; init; }
}
