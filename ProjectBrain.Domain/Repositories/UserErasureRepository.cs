namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class UserErasureRepository : IUserErasureRepository
{
    private readonly AppDbContext _context;

    public UserErasureRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task DeleteRelationalDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _context.CoachMessages
            .Where(cm => cm.UserId == userId || cm.CoachId == userId || cm.SenderId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.Connections
            .Where(c => c.CoachId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.CoachRatings
            .Where(cr => cr.CoachId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.ReferralRewards
            .Where(rr => rr.BeneficiaryUserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.ReferralInvites
            .Where(ri => ri.AcceptedByUserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.SubscriptionExclusions
            .Where(se => se.ExcludedBy == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var journalEntryIds = _context.JournalEntries
            .Where(je => je.UserId == userId)
            .Select(je => je.Id);

        await _context.JournalEntrySystemTags
            .Where(jest => journalEntryIds.Contains(jest.JournalEntryId))
            .ExecuteDeleteAsync(cancellationToken);

        await _context.JournalEntryTags
            .Where(jet => journalEntryIds.Contains(jet.JournalEntryId))
            .ExecuteDeleteAsync(cancellationToken);

        await _context.JournalEntries
            .Where(je => je.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.Tags
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
