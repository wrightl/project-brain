namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for QuizResponse entity
/// </summary>
public class QuizResponseRepository : Repository<QuizResponse, Guid>, IQuizResponseRepository
{
    public QuizResponseRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<QuizResponse?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(qr => qr.Quiz)
            .FirstOrDefaultAsync(qr => qr.Id == id && qr.UserId == userId, cancellationToken);
    }

    public async Task<QuizResponse?> GetByQuizAndUserAsync(Guid quizId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(qr => qr.Quiz)
            .FirstOrDefaultAsync(qr => qr.QuizId == quizId && qr.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<QuizResponse>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(qr => qr.Quiz)
            .Where(qr => qr.UserId == userId)
            .OrderByDescending(qr => qr.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<QuizResponse>> GetPagedForUserAsync(string userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(qr => qr.Quiz)
            .Where(qr => qr.UserId == userId)
            .OrderByDescending(qr => qr.CompletedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<QuizResponse>> GetByQuizForUserAsync(Guid quizId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(qr => qr.Quiz)
            .Where(qr => qr.QuizId == quizId && qr.UserId == userId)
            .OrderByDescending(qr => qr.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<QuizResponse>> GetAllForQuizAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(qr => qr.Quiz)
            .Where(qr => qr.QuizId == quizId)
            .OrderByDescending(qr => qr.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(qr => qr.UserId == userId, cancellationToken);
    }

    public async Task<int> CountForQuizAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(qr => qr.QuizId == quizId, cancellationToken);
    }
}

