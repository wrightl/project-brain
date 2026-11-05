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

        await DeleteCommunityDataAsync(userId, cancellationToken);
    }

    public async Task DeleteCommunityDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Community FKs to Users are Restrict. These rows must be gone before the user
        // row is deleted, or erasure fails after blobs have already been removed.
        var authoredPostIds = await _context.CommunityPosts
            .Where(p => p.AuthorUserId == userId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var reactions = await _context.CommunityReactions
            .Where(r => r.UserId == userId || authoredPostIds.Contains(r.PostId))
            .ToListAsync(cancellationToken);
        var reports = await _context.CommunityReports
            .Where(r => r.ReporterUserId == userId || authoredPostIds.Contains(r.PostId))
            .ToListAsync(cancellationToken);
        var posts = await _context.CommunityPosts
            .Where(p => p.AuthorUserId == userId)
            .ToListAsync(cancellationToken);

        _context.CommunityReactions.RemoveRange(reactions);
        _context.CommunityReports.RemoveRange(reports);
        _context.CommunityPosts.RemoveRange(posts);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
