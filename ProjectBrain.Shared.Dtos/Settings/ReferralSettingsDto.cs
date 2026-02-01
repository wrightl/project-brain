namespace ProjectBrain.Shared.Dtos.Settings;

/// <summary>
/// DTO for referral program settings.
/// </summary>
public class ReferralSettingsDto
{
    public required bool Enabled { get; init; }
    public required int MaxRewardsPerInviter { get; init; }
    public required int InviterFreeMonths { get; init; }
    public required int InviteeFreeMonths { get; init; }
    public required int InviteTokenExpiryDays { get; init; }
    public required int MaxInvitesPerRequest { get; init; }
    public required bool RequireInviterActiveSubscriberToEarn { get; init; }
}

/// <summary>
/// DTO for updating referral program settings.
/// </summary>
public class UpdateReferralSettingsRequestDto
{
    public required bool Enabled { get; init; }
    public required int MaxRewardsPerInviter { get; init; }
    public required int InviterFreeMonths { get; init; }
    public required int InviteeFreeMonths { get; init; }
    public required int InviteTokenExpiryDays { get; init; }
    public required int MaxInvitesPerRequest { get; init; }
    public required bool RequireInviterActiveSubscriberToEarn { get; init; }
}

