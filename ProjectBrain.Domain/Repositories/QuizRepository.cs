namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for Quiz entity
/// </summary>
public class QuizRepository : Repository<Quiz, Guid>, IQuizRepository
{
    public QuizRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Quiz?> GetByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(q => q.Questions.OrderBy(qq => qq.QuestionOrder))
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Quiz>> GetAllOrderedByDateAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Quiz>> GetPagedOrderedByDateAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(q => q.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    public async Task<bool> HasResponsesAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        return await _context.QuizResponses
            .AsNoTracking()
            .AnyAsync(qr => qr.QuizId == quizId, cancellationToken);
    }
}

