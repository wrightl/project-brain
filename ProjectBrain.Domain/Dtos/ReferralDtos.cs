namespace ProjectBrain.Domain.Dtos;

public class ReferralInviteListItemDto
{
    public required Guid Id { get; init; }
    public required string RecipientEmail { get; init; }
    public required string Status { get; init; }
    public required DateTime SentAt { get; init; }
    public DateTime? LastSentAt { get; init; }
    public required int ResendCount { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public DateTime? AcceptedAt { get; init; }
    public DateTime? RewardedAt { get; init; }
}

public class CreateReferralInvitesResultDto
{
    public required List<ReferralInviteListItemDto> Created { get; init; }
    public required List<ReferralInviteSkippedDto> Skipped { get; init; }
}

public class ReferralInviteSkippedDto
{
    public required string RecipientEmail { get; init; }
    public required string Reason { get; init; }
}

public class ReferralInvitePreviewDto
{
    public required string InviterName { get; init; }
    public required int InviteeFreeMonths { get; init; }
    public required bool IsExpired { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

