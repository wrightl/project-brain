namespace ProjectBrain.Shared.Dtos.Settings;

/// <summary>
/// DTO for application setting in API responses
/// </summary>
public class ApplicationSettingDto
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    public required string UpdatedAt { get; init; }
    public required string UpdatedBy { get; init; }
}
