namespace ProjectBrain.Domain;

using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public interface IReferralService
{
    Task<CreateReferralInvitesResultDto> CreateInvitesAsync(
        string inviterUserId,
        string inviterEmail,
        string inviterName,
        IReadOnlyList<string> emails,
        string? baseUrl = null,
        CancellationToken cancellationToken = default);

    Task<List<ReferralInviteListItemDto>> GetInvitesForInviterAsync(
        string inviterUserId,
        CancellationToken cancellationToken = default);

    Task<ReferralInviteListItemDto> ResendInviteAsync(
        string inviterUserId,
        Guid inviteId,
        string inviterEmail,
        string inviterName,
        string? baseUrl = null,
        CancellationToken cancellationToken = default);

    Task<ReferralInvitePreviewDto> PreviewInviteAsync(string token, CancellationToken cancellationToken = default);

    Task AcceptInviteAsync(
        string inviteeUserId,
        string inviteeEmail,
        string token,
        CancellationToken cancellationToken = default);

    Task ProcessInvoicePaymentSucceededAsync(
        string stripeSubscriptionId,
        string? stripeInvoiceId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);
}

public class ReferralService : IReferralService
{
    private readonly ILogger<ReferralService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IReferralSettingsService _referralSettingsService;
    private readonly IReferralInviteRepository _inviteRepository;
    private readonly IReferralRewardRepository _rewardRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStripeService _stripeService;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ReferralService(
        ILogger<ReferralService> logger,
        IConfiguration configuration,
        IEmailService emailService,
        IReferralSettingsService referralSettingsService,
        IReferralInviteRepository inviteRepository,
        IReferralRewardRepository rewardRepository,
        IUserRepository userRepository,
        IStripeService stripeService,
        AppDbContext context,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;
        _referralSettingsService = referralSettingsService;
        _inviteRepository = inviteRepository;
        _rewardRepository = rewardRepository;
        _userRepository = userRepository;
        _stripeService = stripeService;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateReferralInvitesResultDto> CreateInvitesAsync(
        string inviterUserId,
        string inviterEmail,
        string inviterName,
        IReadOnlyList<string> emails,
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await _referralSettingsService.GetReferralSettingsAsync();
        if (!settings.Enabled)
        {
            throw new InvalidOperationException("Referrals are currently disabled.");
        }

        var maxPerRequest = Math.Clamp(settings.MaxInvitesPerRequest, 1, 10);
        if (emails.Count == 0)
        {
            return new CreateReferralInvitesResultDto
            {
                Created = new List<ReferralInviteListItemDto>(),
                Skipped = new List<ReferralInviteSkippedDto>()
            };
        }
        if (emails.Count > maxPerRequest)
        {
            throw new InvalidOperationException($"You can invite up to {maxPerRequest} email addresses at a time.");
        }

        var normalizedDistinct = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedDistinct.Count > maxPerRequest)
        {
            throw new InvalidOperationException($"You can invite up to {maxPerRequest} email addresses at a time.");
        }

        var created = new List<(ReferralInvite Invite, string RawToken)>();
        var createdDtos = new List<ReferralInviteListItemDto>();
        var skipped = new List<ReferralInviteSkippedDto>();

        foreach (var recipientEmail in normalizedDistinct)
        {
            var recipientNormalized = NormalizeEmail(recipientEmail);
            if (string.IsNullOrEmpty(recipientNormalized))
            {
                skipped.Add(new ReferralInviteSkippedDto { RecipientEmail = recipientEmail, Reason = "Invalid email" });
                continue;
            }

            // Prevent self-invites
            if (string.Equals(NormalizeEmail(inviterEmail), recipientNormalized, StringComparison.Ordinal))
            {
                skipped.Add(new ReferralInviteSkippedDto { RecipientEmail = recipientEmail, Reason = "You can’t invite yourself" });
                continue;
            }

            // Don't send invites to email addresses that already belong to an existing account.
            var existingUser = await _userRepository.GetByEmailAsync(recipientNormalized, cancellationToken);
            if (existingUser != null)
            {
                skipped.Add(new ReferralInviteSkippedDto { RecipientEmail = recipientEmail, Reason = "Already has an account" });
                continue;
            }

            var existing = await _inviteRepository.GetByInviterAndRecipientAsync(inviterUserId, recipientNormalized, cancellationToken);
            if (existing != null)
            {
                skipped.Add(new ReferralInviteSkippedDto { RecipientEmail = recipientEmail, Reason = "Already invited" });
                continue;
            }

            var rawToken = GenerateToken();
            var tokenHash = HashToken(rawToken);

            var invite = new ReferralInvite
            {
                Id = Guid.NewGuid(),
                InviterUserId = inviterUserId,
                RecipientEmail = recipientEmail,
                RecipientEmailNormalized = recipientNormalized,
                TokenHash = tokenHash,
                Status = "Pending",
                SentAt = DateTime.UtcNow,
                LastSentAt = null,
                ResendCount = 0,
                ExpiresAt = DateTime.UtcNow.AddDays(Math.Max(1, settings.InviteTokenExpiryDays)),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _inviteRepository.Add(invite);
            created.Add((invite, rawToken));
        }

        await _unitOfWork.SaveChangesAsync();

        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException("Base URL is required");
        }

        foreach (var (invite, rawToken) in created)
        {
            var acceptUrl = BuildAcceptUrl(baseUrl, rawToken);
            var subject = $"{inviterName} invited you to ProjectBrain";

            var inviteeMonths = settings.InviteeFreeMonths;
            var inviteeBenefit = inviteeMonths > 0
                ? $"{inviteeMonths} month{(inviteeMonths == 1 ? "" : "s")} free"
                : "a referral reward";

            var htmlBody = $@"
<div style=""font-family: Arial, sans-serif; line-height: 1.5;"">
  <h2>You’ve been invited to ProjectBrain</h2>
  <p><strong>{EscapeHtml(inviterName)}</strong> invited you to try ProjectBrain.</p>
  <p>If you join and become a paying subscriber (after any free trial), you’ll receive <strong>{EscapeHtml(inviteeBenefit)}</strong>.</p>
  <p><a href=""{acceptUrl}"">Accept your invite</a></p>
  <p style=""color:#666;font-size:12px;"">This invite link expires on {invite.ExpiresAt:yyyy-MM-dd}.</p>
</div>";

            var textBody =
                $"You’ve been invited to ProjectBrain by {inviterName}.\n\n" +
                $"If you join and become a paying subscriber (after any free trial), you’ll receive {inviteeBenefit}.\n\n" +
                $"Accept your invite: {acceptUrl}\n\n" +
                $"This invite link expires on {invite.ExpiresAt:yyyy-MM-dd}.";

            await _emailService.SendEmailAsync(
                to: invite.RecipientEmail,
                subject: subject,
                htmlBody: htmlBody,
                textBody: textBody);

            createdDtos.Add(ToListItemDto(invite));
        }

        return new CreateReferralInvitesResultDto
        {
            Created = createdDtos,
            Skipped = skipped
        };
    }

    public async Task<List<ReferralInviteListItemDto>> GetInvitesForInviterAsync(
        string inviterUserId,
        CancellationToken cancellationToken = default)
    {
        var invites = await _inviteRepository.ListForInviterAsync(inviterUserId, cancellationToken);
        return invites.Select(ToListItemDto).ToList();
    }

    public async Task<ReferralInviteListItemDto> ResendInviteAsync(
        string inviterUserId,
        Guid inviteId,
        string inviterEmail,
        string inviterName,
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await _referralSettingsService.GetReferralSettingsAsync();
        if (!settings.Enabled)
        {
            throw new InvalidOperationException("Referrals are currently disabled.");
        }

        var invite = await _context.ReferralInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.InviterUserId == inviterUserId, cancellationToken);

        if (invite == null)
        {
            throw new InvalidOperationException("Invite not found.");
        }

        if (invite.AcceptedAt != null || invite.Status == "Accepted" || invite.Status == "Rewarded")
        {
            throw new InvalidOperationException("This invite has already been accepted.");
        }

        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            invite.Status = "Expired";
            invite.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            throw new InvalidOperationException("This invite has expired.");
        }

        // Issue a new token on resend (we do not store raw tokens)
        var rawToken = GenerateToken();
        invite.TokenHash = HashToken(rawToken);
        invite.ResendCount += 1;
        invite.LastSentAt = DateTime.UtcNow;
        invite.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException("Base URL is required");
        }
        var acceptUrl = BuildAcceptUrl(baseUrl, rawToken);

        var inviteeMonths = settings.InviteeFreeMonths;
        var inviteeBenefit = inviteeMonths > 0
            ? $"{inviteeMonths} month{(inviteeMonths == 1 ? "" : "s")} free"
            : "a referral reward";

        await _emailService.SendEmailAsync(
            to: invite.RecipientEmail,
            subject: $"{inviterName} invited you to ProjectBrain",
            htmlBody: $@"
<div style=""font-family: Arial, sans-serif; line-height: 1.5;"">
  <h2>You’ve been invited to ProjectBrain</h2>
  <p><strong>{EscapeHtml(inviterName)}</strong> invited you to try ProjectBrain.</p>
  <p>If you join and become a paying subscriber (after any free trial), you’ll receive <strong>{EscapeHtml(inviteeBenefit)}</strong>.</p>
  <p><a href=""{acceptUrl}"">Accept your invite</a></p>
  <p style=""color:#666;font-size:12px;"">This invite link expires on {invite.ExpiresAt:yyyy-MM-dd}.</p>
</div>",
            textBody:
                $"You’ve been invited to ProjectBrain by {inviterName}.\n\n" +
                $"If you join and become a paying subscriber (after any free trial), you’ll receive {inviteeBenefit}.\n\n" +
                $"Accept your invite: {acceptUrl}\n\n" +
                $"This invite link expires on {invite.ExpiresAt:yyyy-MM-dd}."
        );

        return ToListItemDto(invite);
    }

    public async Task<ReferralInvitePreviewDto> PreviewInviteAsync(string token, CancellationToken cancellationToken = default)
    {
        var settings = await _referralSettingsService.GetReferralSettingsAsync();
        if (!settings.Enabled)
        {
            throw new InvalidOperationException("Referrals are currently disabled.");
        }

        var tokenHash = HashToken(token);
        var invite = await _inviteRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (invite == null)
        {
            throw new InvalidOperationException("Invite not found.");
        }

        var isExpired = invite.ExpiresAt < DateTime.UtcNow || invite.Status == "Expired";

        return new ReferralInvitePreviewDto
        {
            InviterName = invite.Inviter?.FullName ?? "A ProjectBrain user",
            InviteeFreeMonths = settings.InviteeFreeMonths,
            IsExpired = isExpired,
            ExpiresAt = invite.ExpiresAt
        };
    }

    public async Task AcceptInviteAsync(
        string inviteeUserId,
        string inviteeEmail,
        string token,
        CancellationToken cancellationToken = default)
    {
        var settings = await _referralSettingsService.GetReferralSettingsAsync();
        if (!settings.Enabled)
        {
            throw new InvalidOperationException("Referrals are currently disabled.");
        }

        // Prevent a single recipient from accepting multiple referral invites (anti-abuse).
        var alreadyAcceptedAnyInvite = await _context.ReferralInvites
            .AsNoTracking()
            .AnyAsync(ri => ri.AcceptedByUserId == inviteeUserId, cancellationToken);
        if (alreadyAcceptedAnyInvite)
        {
            throw new InvalidOperationException("You have already accepted a referral invite.");
        }

        var tokenHash = HashToken(token);
        var invite = await _context.ReferralInvites
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

        if (invite == null)
        {
            throw new InvalidOperationException("Invite not found.");
        }

        if (invite.ExpiresAt < DateTime.UtcNow || invite.Status == "Expired")
        {
            invite.Status = "Expired";
            invite.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            throw new InvalidOperationException("This invite has expired.");
        }

        if (invite.AcceptedAt != null || invite.Status == "Accepted" || invite.Status == "Rewarded")
        {
            throw new InvalidOperationException("This invite has already been accepted.");
        }

        var inviteeEmailNormalized = NormalizeEmail(inviteeEmail);
        if (!string.Equals(inviteeEmailNormalized, invite.RecipientEmailNormalized, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("This invite was sent to a different email address.");
        }

        invite.Status = "Accepted";
        invite.AcceptedAt = DateTime.UtcNow;
        invite.AcceptedByUserId = inviteeUserId;
        invite.UpdatedAt = DateTime.UtcNow;

        // Create reward records (pending). Rewards are only applied after invitee becomes paid beyond trial.
        var inviteeMonths = Math.Max(0, settings.InviteeFreeMonths);
        if (inviteeMonths > 0)
        {
            var existingInviteeReward = await _rewardRepository.GetForInviteAndBeneficiaryAsync(invite.Id, inviteeUserId, cancellationToken);
            if (existingInviteeReward == null)
            {
                _context.ReferralRewards.Add(new ReferralReward
                {
                    Id = Guid.NewGuid(),
                    ReferralInviteId = invite.Id,
                    BeneficiaryUserId = inviteeUserId,
                    Months = inviteeMonths,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        var inviterMonths = Math.Max(0, settings.InviterFreeMonths);
        if (inviterMonths > 0 && settings.MaxRewardsPerInviter != 0)
        {
            // Soft enforcement: if they already hit max applied, don't create inviter reward.
            var alreadyApplied = await _rewardRepository.CountAppliedForBeneficiaryAsync(invite.InviterUserId, cancellationToken);
            if (settings.MaxRewardsPerInviter < 0 || alreadyApplied < settings.MaxRewardsPerInviter)
            {
                var existingInviterReward = await _rewardRepository.GetForInviteAndBeneficiaryAsync(invite.Id, invite.InviterUserId, cancellationToken);
                if (existingInviterReward == null)
                {
                    _context.ReferralRewards.Add(new ReferralReward
                    {
                        Id = Guid.NewGuid(),
                        ReferralInviteId = invite.Id,
                        BeneficiaryUserId = invite.InviterUserId,
                        Months = inviterMonths,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Referral invite {InviteId} accepted by user {InviteeUserId}",
            invite.Id,
            inviteeUserId);
    }

    public Task ProcessInvoicePaymentSucceededAsync(
        string stripeSubscriptionId,
        string? stripeInvoiceId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        return ProcessInvoicePaymentSucceededInternalAsync(stripeSubscriptionId, stripeInvoiceId, occurredAtUtc, cancellationToken);
    }

    private async Task ProcessInvoicePaymentSucceededInternalAsync(
        string stripeSubscriptionId,
        string? stripeInvoiceId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var settings = await _referralSettingsService.GetReferralSettingsAsync();
        if (!settings.Enabled)
        {
            return;
        }

        // Identify who just paid
        var paidSubscription = await _context.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(us => us.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);

        if (paidSubscription == null)
        {
            return;
        }

        // Referral program is users-only
        if (!string.Equals(paidSubscription.UserType, UserType.User.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Must be paid beyond trial
        if (!string.Equals(paidSubscription.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (paidSubscription.TrialEndsAt != null && paidSubscription.TrialEndsAt > occurredAtUtc)
        {
            return;
        }

        var inviteeUserId = paidSubscription.UserId;

        // Find the accepted invite for this invitee (should only ever be one accepted invite per invitee)
        var invite = await _context.ReferralInvites
            .FirstOrDefaultAsync(
                ri => ri.AcceptedByUserId == inviteeUserId && (ri.Status == "Accepted" || ri.Status == "Rewarded"),
                cancellationToken);

        if (invite != null)
        {
            // Mark invite as rewarded once invitee becomes paid beyond trial
            if (invite.Status != "Rewarded")
            {
                invite.Status = "Rewarded";
                invite.RewardedAt = occurredAtUtc;
                invite.UpdatedAt = DateTime.UtcNow;
            }

            // Apply invitee reward first (invitee is definitely subscribed at this point)
            await TryApplyRewardAsync(
                referralInviteId: invite.Id,
                beneficiaryUserId: inviteeUserId,
                months: Math.Max(0, settings.InviteeFreeMonths),
                triggeringStripeInvoiceId: stripeInvoiceId,
                cancellationToken: cancellationToken);

            // Inviter reward: enforce max rewards and apply only if inviter has an eligible paid subscription
            if (settings.MaxRewardsPerInviter != 0 && settings.InviterFreeMonths > 0)
            {
                var alreadyApplied = await _rewardRepository.CountAppliedForBeneficiaryAsync(invite.InviterUserId, cancellationToken);
                var underLimit = settings.MaxRewardsPerInviter < 0 || alreadyApplied < settings.MaxRewardsPerInviter;
                if (underLimit)
                {
                    // If required, inviter must be paid (active, trial ended) to apply now.
                    var inviterEligibleNow = await HasEligiblePaidSubscriptionAsync(invite.InviterUserId, occurredAtUtc, cancellationToken);
                    if (!settings.RequireInviterActiveSubscriberToEarn || inviterEligibleNow)
                    {
                        await TryApplyRewardAsync(
                            referralInviteId: invite.Id,
                            beneficiaryUserId: invite.InviterUserId,
                            months: Math.Max(0, settings.InviterFreeMonths),
                            triggeringStripeInvoiceId: stripeInvoiceId,
                            cancellationToken: cancellationToken);
                    }
                }
            }
        }

        // If this paying user has any earned pending rewards (e.g. they are an inviter),
        // apply them now that they have an eligible paid subscription.
        await ApplyEarnedPendingRewardsForBeneficiaryAsync(
            beneficiaryUserId: inviteeUserId,
            triggeringStripeInvoiceId: stripeInvoiceId,
            asOfUtc: occurredAtUtc,
            maxRewardsPerInviter: settings.MaxRewardsPerInviter,
            requireInviterActiveSubscriberToEarn: settings.RequireInviterActiveSubscriberToEarn,
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<bool> HasEligiblePaidSubscriptionAsync(string userId, DateTime asOfUtc, CancellationToken cancellationToken)
    {
        var subscription = await _context.UserSubscriptions
            .AsNoTracking()
            .Where(us => us.UserId == userId && string.Equals(us.UserType, UserType.User.ToString(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(us => us.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            return false;
        }

        if (!string.Equals(subscription.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (subscription.TrialEndsAt != null && subscription.TrialEndsAt > asOfUtc)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId);
    }

    private async Task TryApplyRewardAsync(
        Guid referralInviteId,
        string beneficiaryUserId,
        int months,
        string? triggeringStripeInvoiceId,
        CancellationToken cancellationToken)
    {
        if (months <= 0)
        {
            return;
        }

        var reward = await _context.ReferralRewards
            .FirstOrDefaultAsync(
                rr => rr.ReferralInviteId == referralInviteId && rr.BeneficiaryUserId == beneficiaryUserId,
                cancellationToken);

        if (reward == null)
        {
            // Create pending reward if it wasn't created at accept time
            reward = new ReferralReward
            {
                Id = Guid.NewGuid(),
                ReferralInviteId = referralInviteId,
                BeneficiaryUserId = beneficiaryUserId,
                Months = months,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ReferralRewards.Add(reward);
        }

        if (string.Equals(reward.Status, "Applied", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Find beneficiary subscription to extend
        var subscription = await _context.UserSubscriptions
            .AsNoTracking()
            .Where(us => us.UserId == beneficiaryUserId && string.Equals(us.UserType, UserType.User.ToString(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(us => us.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null || string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
        {
            // Can't apply yet
            return;
        }

        if (!string.Equals(subscription.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            // Only extend paying subscriptions
            return;
        }

        // Apply in Stripe
        var newPeriodEnd = await _stripeService.ExtendSubscriptionByMonthsAsync(subscription.StripeSubscriptionId, months);

        // Update local subscription record (best-effort sync)
        var trackedSub = await _context.UserSubscriptions
            .FirstOrDefaultAsync(us => us.Id == subscription.Id, cancellationToken);
        if (trackedSub != null)
        {
            trackedSub.CurrentPeriodEnd = newPeriodEnd;
            trackedSub.UpdatedAt = DateTime.UtcNow;
        }

        reward.Status = "Applied";
        reward.AppliedAt = DateTime.UtcNow;
        reward.AppliedToStripeSubscriptionId = subscription.StripeSubscriptionId;
        reward.TriggeringStripeInvoiceId ??= triggeringStripeInvoiceId;
        reward.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Applied referral reward {RewardId} to user {UserId} for {Months} month(s)",
            reward.Id,
            beneficiaryUserId,
            months);
    }

    private async Task ApplyEarnedPendingRewardsForBeneficiaryAsync(
        string beneficiaryUserId,
        string? triggeringStripeInvoiceId,
        DateTime asOfUtc,
        int maxRewardsPerInviter,
        bool requireInviterActiveSubscriberToEarn,
        CancellationToken cancellationToken)
    {
        // Beneficiary must be eligible/paid now; if not, skip
        if (!await HasEligiblePaidSubscriptionAsync(beneficiaryUserId, asOfUtc, cancellationToken))
        {
            return;
        }

        var pendingRewards = await _context.ReferralRewards
            .Include(rr => rr.ReferralInvite)
            .Where(rr => rr.BeneficiaryUserId == beneficiaryUserId && rr.Status == "Pending")
            .OrderBy(rr => rr.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var reward in pendingRewards)
        {
            var invite = reward.ReferralInvite;
            if (invite == null)
            {
                continue;
            }

            // Only apply rewards once the referral has been earned (invitee paid beyond trial)
            if (invite.Status != "Rewarded")
            {
                continue;
            }

            // Inviter-specific gating
            if (string.Equals(invite.InviterUserId, beneficiaryUserId, StringComparison.OrdinalIgnoreCase))
            {
                if (maxRewardsPerInviter == 0)
                {
                    continue;
                }

                var alreadyApplied = await _rewardRepository.CountAppliedForBeneficiaryAsync(beneficiaryUserId, cancellationToken);
                if (!(maxRewardsPerInviter < 0 || alreadyApplied < maxRewardsPerInviter))
                {
                    continue;
                }

                if (requireInviterActiveSubscriberToEarn && !await HasEligiblePaidSubscriptionAsync(beneficiaryUserId, asOfUtc, cancellationToken))
                {
                    continue;
                }
            }

            await TryApplyRewardAsync(
                referralInviteId: reward.ReferralInviteId,
                beneficiaryUserId: beneficiaryUserId,
                months: reward.Months,
                triggeringStripeInvoiceId: triggeringStripeInvoiceId,
                cancellationToken: cancellationToken);
        }
    }

    private static ReferralInviteListItemDto ToListItemDto(ReferralInvite invite)
    {
        return new ReferralInviteListItemDto
        {
            Id = invite.Id,
            RecipientEmail = invite.RecipientEmail,
            Status = invite.Status,
            SentAt = invite.SentAt,
            LastSentAt = invite.LastSentAt,
            ResendCount = invite.ResendCount,
            ExpiresAt = invite.ExpiresAt,
            AcceptedAt = invite.AcceptedAt,
            RewardedAt = invite.RewardedAt
        };
    }

    private static string BuildAcceptUrl(string baseUrl, string rawToken)
    {
        var tokenEncoded = Uri.EscapeDataString(rawToken);
        return $"{baseUrl}/referral/accept?token={tokenEncoded}";
    }

    private static string NormalizeEmail(string email)
    {
        var trimmed = email.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        try
        {
            var parsed = new MailAddress(trimmed);
            // MailAddress can normalize; ensure we keep a clean address string.
            return parsed.Address.Trim().ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GenerateToken()
    {
        // 32 bytes -> 64 hex chars
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string EscapeHtml(string input)
    {
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}

