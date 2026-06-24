namespace ProjectBrain.Shared.Dtos.Settings;

/// <summary>Single admin-managed chat policy setting.</summary>
public class ChatPolicySettingDto
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? Description { get; init; }
}

/// <summary>DTO for chat policy settings collection.</summary>
public class ChatPolicySettingsDto
{
    public required List<ChatPolicySettingDto> Policies { get; init; }
}

/// <summary>Request DTO for updating chat policy settings.</summary>
public class UpdateChatPolicySettingsRequestDto
{
    public required List<ChatPolicySettingDto> Policies { get; init; }
}
