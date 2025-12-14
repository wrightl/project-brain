namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class QuizService : IQuizService
{
    private readonly IQuizRepository _repository;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public QuizService(IQuizRepository repository, AppDbContext context, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<Quiz> Add(Quiz quiz)
    {
        _repository.Add(quiz);
        await _unitOfWork.SaveChangesAsync();
        return quiz;
    }

    public async Task<Quiz?> GetById(Guid id)
    {
        return await _repository.GetByIdWithQuestionsAsync(id);
    }

    public async Task<IEnumerable<Quiz>> GetAll()
    {
        return await _repository.GetAllOrderedByDateAsync();
    }

    public async Task<Quiz> Update(Quiz quiz)
    {
        _repository.Update(quiz);
        await _unitOfWork.SaveChangesAsync();
        return quiz;
    }

    public async Task<bool> Delete(Guid id)
    {
        // Get tracked entity for deletion (not using AsNoTracking)
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null)
        {
            return false;
        }

        _repository.Remove(quiz);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasResponses(Guid quizId)
    {
        return await _repository.HasResponsesAsync(quizId);
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

