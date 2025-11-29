namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;

public class VoiceNoteService : IVoiceNoteService
{
    private readonly AppDbContext _context;

    public VoiceNoteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VoiceNote> Add(VoiceNote voiceNote)
    {
        _context.VoiceNotes.Add(voiceNote);
        await _context.SaveChangesAsync();
        return voiceNote;
    }

    public async Task<VoiceNote?> GetById(Guid id, string userId)
    {
        return await _context.VoiceNotes
            .FirstOrDefaultAsync(vn => vn.Id == id && vn.UserId == userId);
    }

    public async Task<IEnumerable<VoiceNote>> GetAllForUser(string userId)
    {
        return await _context.VoiceNotes
            .Where(vn => vn.UserId == userId)
            .OrderByDescending(vn => vn.CreatedAt)
            .ToListAsync();
    }

    public async Task<VoiceNote> Update(VoiceNote voiceNote)
    {
        _context.VoiceNotes.Update(voiceNote);
        await _context.SaveChangesAsync();
        return voiceNote;
    }

    public async Task<bool> Delete(Guid id, string userId)
    {
        var voiceNote = await GetById(id, userId);
        if (voiceNote == null)
        {
            return false;
        }

        _context.VoiceNotes.Remove(voiceNote);
        await _context.SaveChangesAsync();
        return true;
    }
}

public interface IVoiceNoteService
{
    Task<VoiceNote> Add(VoiceNote voiceNote);
    Task<VoiceNote?> GetById(Guid id, string userId);
    Task<IEnumerable<VoiceNote>> GetAllForUser(string userId);
    Task<VoiceNote> Update(VoiceNote voiceNote);
    Task<bool> Delete(Guid id, string userId);
}

