namespace ProjectBrain.Domain;

public class ReferralSettings
{
    public bool Enabled { get; set; } = false;
    public int MaxRewardsPerInviter { get; set; } = 12;
    public int InviterFreeMonths { get; set; } = 1;
    public int InviteeFreeMonths { get; set; } = 1;
    public int InviteTokenExpiryDays { get; set; } = 30;
    public int MaxInvitesPerRequest { get; set; } = 10;
    public bool RequireInviterActiveSubscriberToEarn { get; set; } = false;
}

public interface IReferralSettingsService
{
    Task<ReferralSettings> GetReferralSettingsAsync();
}

public class ReferralSettingsService : IReferralSettingsService
{
    private readonly IApplicationSettingsService _applicationSettingsService;

    private const string ReferralEnabledKey = "Referral:Enabled";
    private const string ReferralMaxRewardsPerInviterKey = "Referral:MaxRewardsPerInviter";
    private const string ReferralInviterFreeMonthsKey = "Referral:InviterFreeMonths";
    private const string ReferralInviteeFreeMonthsKey = "Referral:InviteeFreeMonths";
    private const string ReferralInviteTokenExpiryDaysKey = "Referral:InviteTokenExpiryDays";
    private const string ReferralMaxInvitesPerRequestKey = "Referral:MaxInvitesPerRequest";
    private const string ReferralRequireInviterActiveSubscriberToEarnKey = "Referral:RequireInviterActiveSubscriberToEarn";

    public ReferralSettingsService(IApplicationSettingsService applicationSettingsService)
    {
        _applicationSettingsService = applicationSettingsService;
    }

    public async Task<ReferralSettings> GetReferralSettingsAsync()
    {
        var enabledRaw = await _applicationSettingsService.GetSettingAsync(ReferralEnabledKey);
        var maxRewardsRaw = await _applicationSettingsService.GetSettingAsync(ReferralMaxRewardsPerInviterKey);
        var inviterMonthsRaw = await _applicationSettingsService.GetSettingAsync(ReferralInviterFreeMonthsKey);
        var inviteeMonthsRaw = await _applicationSettingsService.GetSettingAsync(ReferralInviteeFreeMonthsKey);
        var expiryDaysRaw = await _applicationSettingsService.GetSettingAsync(ReferralInviteTokenExpiryDaysKey);
        var maxInvitesRaw = await _applicationSettingsService.GetSettingAsync(ReferralMaxInvitesPerRequestKey);
        var requireInviterPaidRaw = await _applicationSettingsService.GetSettingAsync(ReferralRequireInviterActiveSubscriberToEarnKey);

        return new ReferralSettings
        {
            Enabled = bool.TryParse(enabledRaw, out var enabled) ? enabled : false,
            MaxRewardsPerInviter = int.TryParse(maxRewardsRaw, out var maxRewards) ? maxRewards : 12,
            InviterFreeMonths = int.TryParse(inviterMonthsRaw, out var inviterMonths) ? inviterMonths : 1,
            InviteeFreeMonths = int.TryParse(inviteeMonthsRaw, out var inviteeMonths) ? inviteeMonths : 1,
            InviteTokenExpiryDays = int.TryParse(expiryDaysRaw, out var expiryDays) ? expiryDays : 30,
            MaxInvitesPerRequest = int.TryParse(maxInvitesRaw, out var maxInvites) ? maxInvites : 10,
            RequireInviterActiveSubscriberToEarn =
                bool.TryParse(requireInviterPaidRaw, out var requireInviterPaid) ? requireInviterPaid : false
        };
    }
}

