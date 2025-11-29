namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;

public class QuizResponseService : IQuizResponseService
{
    private readonly AppDbContext _context;

    public QuizResponseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<QuizResponse> Add(QuizResponse response)
    {
        _context.QuizResponses.Add(response);
        await _context.SaveChangesAsync();
        return response;
    }

    public async Task<QuizResponse?> GetById(Guid id, string userId)
    {
        return await _context.QuizResponses
            .Include(qr => qr.Quiz)
            .FirstOrDefaultAsync(qr => qr.Id == id && qr.UserId == userId);
    }

    public async Task<QuizResponse?> GetByQuizAndUser(Guid quizId, string userId)
    {
        return await _context.QuizResponses
            .Include(qr => qr.Quiz)
            .FirstOrDefaultAsync(qr => qr.QuizId == quizId && qr.UserId == userId);
    }

    public async Task<IEnumerable<QuizResponse>> GetAllForUser(string userId)
    {
        return await _context.QuizResponses
            .Include(qr => qr.Quiz)
            .Where(qr => qr.UserId == userId)
            .OrderByDescending(qr => qr.CompletedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<QuizResponse>> GetByQuizForUser(Guid quizId, string userId)
    {
        return await _context.QuizResponses
            .Include(qr => qr.Quiz)
            .Where(qr => qr.QuizId == quizId && qr.UserId == userId)
            .OrderByDescending(qr => qr.CompletedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<QuizResponse>> GetAllForQuiz(Guid quizId)
    {
        return await _context.QuizResponses
            .Include(qr => qr.Quiz)
            .Where(qr => qr.QuizId == quizId)
            .OrderByDescending(qr => qr.CompletedAt)
            .ToListAsync();
    }

    public async Task<QuizResponse> Update(QuizResponse response)
    {
        _context.QuizResponses.Update(response);
        await _context.SaveChangesAsync();
        return response;
    }

    public async Task<bool> Delete(Guid id, string userId)
    {
        var response = await GetById(id, userId);
        if (response == null)
        {
            return false;
        }

        _context.QuizResponses.Remove(response);
        await _context.SaveChangesAsync();
        return true;
    }
}

public interface IQuizResponseService
{
    Task<QuizResponse> Add(QuizResponse response);
    Task<QuizResponse?> GetById(Guid id, string userId);
    Task<QuizResponse?> GetByQuizAndUser(Guid quizId, string userId);
    Task<IEnumerable<QuizResponse>> GetAllForUser(string userId);
    Task<IEnumerable<QuizResponse>> GetByQuizForUser(Guid quizId, string userId);
    Task<IEnumerable<QuizResponse>> GetAllForQuiz(Guid quizId);
    Task<QuizResponse> Update(QuizResponse response);
    Task<bool> Delete(Guid id, string userId);
}

