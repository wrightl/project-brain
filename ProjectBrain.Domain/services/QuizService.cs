namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;

public class QuizService : IQuizService
{
    private readonly AppDbContext _context;

    public QuizService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Quiz> Add(Quiz quiz)
    {
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();
        return quiz;
    }

    public async Task<Quiz?> GetById(Guid id)
    {
        return await _context.Quizzes
            .Include(q => q.Questions.OrderBy(qq => qq.QuestionOrder))
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<IEnumerable<Quiz>> GetAll()
    {
        return await _context.Quizzes
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<Quiz> Update(Quiz quiz)
    {
        _context.Quizzes.Update(quiz);
        await _context.SaveChangesAsync();
        return quiz;
    }

    public async Task<bool> Delete(Guid id)
    {
        var quiz = await GetById(id);
        if (quiz == null)
        {
            return false;
        }

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasResponses(Guid quizId)
    {
        return await _context.QuizResponses
            .AnyAsync(qr => qr.QuizId == quizId);
    }
}

public interface IQuizService
{
    Task<Quiz> Add(Quiz quiz);
    Task<Quiz?> GetById(Guid id);
    Task<IEnumerable<Quiz>> GetAll();
    Task<Quiz> Update(Quiz quiz);
    Task<bool> Delete(Guid id);
    Task<bool> HasResponses(Guid quizId);
}

