namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class VoiceNoteService : IVoiceNoteService
{
    private readonly IVoiceNoteRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public VoiceNoteService(IVoiceNoteRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<VoiceNote> Add(VoiceNote voiceNote)
    {
        _repository.Add(voiceNote);
        await _unitOfWork.SaveChangesAsync();
        return voiceNote;
    }

    public async Task<VoiceNote?> GetById(Guid id, string userId)
    {
        return await _repository.GetByIdForUserAsync(id, userId);
    }

    public async Task<IEnumerable<VoiceNote>> GetAllForUser(string userId, int? limit = null)
    {
        return await _repository.GetAllForUserAsync(userId, limit);
    }

    public async Task<VoiceNote> Update(VoiceNote voiceNote)
    {
        _repository.Update(voiceNote);
        await _unitOfWork.SaveChangesAsync();
        return voiceNote;
    }

    public async Task<bool> Delete(Guid id, string userId)
    {
        var voiceNote = await GetById(id, userId);
        if (voiceNote == null)
        {
            return false;
        }

        _repository.Remove(voiceNote);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

public interface IVoiceNoteService
{
    Task<VoiceNote> Add(VoiceNote voiceNote);
    Task<VoiceNote?> GetById(Guid id, string userId);
    Task<IEnumerable<VoiceNote>> GetAllForUser(string userId, int? limit = null);
    Task<VoiceNote> Update(VoiceNote voiceNote);
    Task<bool> Delete(Guid id, string userId);
}

