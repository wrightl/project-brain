namespace ProjectBrain.Shared.Dtos.Settings;

/// <summary>
/// DTO for updating an application setting
/// </summary>
public class UpdateApplicationSettingRequestDto
{
    public required string Value { get; init; }
}
