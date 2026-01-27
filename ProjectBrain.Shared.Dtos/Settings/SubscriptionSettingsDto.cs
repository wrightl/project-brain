namespace ProjectBrain.Shared.Dtos.Settings;

/// <summary>
/// DTO for subscription settings
/// </summary>
public class SubscriptionSettingsDto
{
    public required bool EnableUserSubscriptions { get; init; }
    public required bool EnableCoachSubscriptions { get; init; }
}

/// <summary>
/// DTO for updating subscription settings
/// </summary>
public class UpdateSubscriptionSettingsRequestDto
{
    public required bool EnableUserSubscriptions { get; init; }
    public required bool EnableCoachSubscriptions { get; init; }
}

