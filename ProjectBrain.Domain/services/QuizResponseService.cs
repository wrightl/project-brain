namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class QuizResponseService : IQuizResponseService
{
    private readonly IQuizResponseRepository _repository;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public QuizResponseService(IQuizResponseRepository repository, AppDbContext context, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<QuizResponse> Add(QuizResponse response)
    {
        _repository.Add(response);
        await _unitOfWork.SaveChangesAsync();
        return response;
    }

    public async Task<QuizResponse?> GetById(Guid id, string userId)
    {
        return await _repository.GetByIdForUserAsync(id, userId);
    }

    public async Task<QuizResponse?> GetByQuizAndUser(Guid quizId, string userId)
    {
        return await _repository.GetByQuizAndUserAsync(quizId, userId);
    }

    public async Task<IEnumerable<QuizResponse>> GetAllForUser(string userId)
    {
        return await _repository.GetAllForUserAsync(userId);
    }

    public async Task<IEnumerable<QuizResponse>> GetByQuizForUser(Guid quizId, string userId)
    {
        return await _repository.GetByQuizForUserAsync(quizId, userId);
    }

    public async Task<IEnumerable<QuizResponse>> GetAllForQuiz(Guid quizId)
    {
        return await _repository.GetAllForQuizAsync(quizId);
    }

    public async Task<int> CountForUser(string userId)
    {
        return await _repository.CountForUserAsync(userId);
    }

    public async Task<QuizResponse> Update(QuizResponse response)
    {
        _repository.Update(response);
        await _unitOfWork.SaveChangesAsync();
        return response;
    }

    public async Task<bool> Delete(Guid id, string userId)
    {
        // Get tracked entity for deletion (not using AsNoTracking)
        var response = await _context.QuizResponses
            .FirstOrDefaultAsync(qr => qr.Id == id && qr.UserId == userId);
        if (response == null)
        {
            return false;
        }

        _repository.Remove(response);
        await _unitOfWork.SaveChangesAsync();
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
    Task<int> CountForUser(string userId);
    Task<QuizResponse> Update(QuizResponse response);
    Task<bool> Delete(Guid id, string userId);
}

