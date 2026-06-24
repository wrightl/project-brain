namespace ProjectBrain.Shared.Dtos.Settings;

public class FeatureFlagItemDto
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required string Description { get; init; }
    public required bool Enabled { get; init; }
}

public class FeatureFlagSettingsDto
{
    public required IReadOnlyList<FeatureFlagItemDto> Flags { get; init; }
}

public class UpdateFeatureFlagSettingsRequestDto
{
    public required Dictionary<string, bool> Flags { get; init; }
}
