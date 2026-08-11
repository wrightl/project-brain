namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class JournalEntryService : IJournalEntryService
{
    private readonly IJournalEntryRepository _repository;
    private readonly ITagRepository _tagRepository;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public JournalEntryService(
        IJournalEntryRepository repository,
        ITagRepository tagRepository,
        AppDbContext context,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<JournalEntry> Add(
        JournalEntry journalEntry,
        IEnumerable<Guid>? tagIds = null,
        IEnumerable<SystemTagAssignment>? systemTagAssignments = null)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        _repository.Add(journalEntry);

        if (tagIds != null && tagIds.Any())
        {
            var tags = await _tagRepository.GetByIdsForUserAsync(tagIds, journalEntry.UserId);
            foreach (var tag in tags)
            {
                _context.JournalEntryTags.Add(new JournalEntryTag
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = journalEntry.Id,
                    TagId = tag.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (systemTagAssignments != null)
        {
            var assignments = systemTagAssignments
                .Where(a => a.SystemTagId != Guid.Empty)
                .GroupBy(a => a.SystemTagId)
                .Select(g => g.First())
                .ToList();

            if (assignments.Count > 0)
            {
                var ids = assignments.Select(a => a.SystemTagId).ToList();
                var existingIds = await _context.SystemTags
                    .AsNoTracking()
                    .Where(st => ids.Contains(st.Id))
                    .Select(st => st.Id)
                    .ToListAsync();

                var missing = ids.Except(existingIds).ToList();
                if (missing.Count > 0)
                {
                    throw new AppException("INVALID_SYSTEM_TAG", $"Unknown system tag(s): {string.Join(", ", missing)}", 400);
                }

                foreach (var assignment in assignments)
                {
                    _context.JournalEntrySystemTags.Add(new JournalEntrySystemTag
                    {
                        Id = Guid.NewGuid(),
                        JournalEntryId = journalEntry.Id,
                        SystemTagId = assignment.SystemTagId,
                        ResponsesJson = assignment.ResponsesJson,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _unitOfWork.CommitTransactionAsync();
        return journalEntry;
    }

    public async Task<JournalEntry?> GetById(Guid id, string userId)
    {
        return await _repository.GetByIdForUserAsync(id, userId);
    }

    public async Task<JournalEntry?> GetByIdWithTags(Guid id, string userId)
    {
        return await _repository.GetByIdWithTagsForUserAsync(id, userId);
    }

    public async Task<IEnumerable<JournalEntry>> GetAllForUser(string userId)
    {
        return await _repository.GetAllForUserAsync(userId);
    }

    public async Task<IEnumerable<JournalEntry>> GetPagedForUser(string userId, int skip, int take)
    {
        return await _repository.GetPagedForUserAsync(userId, skip, take);
    }

    public async Task<IEnumerable<JournalEntry>> GetRecentForUser(string userId, int count)
    {
        return await _repository.GetRecentForUserAsync(userId, count);
    }

    public async Task<int> CountForUser(string userId)
    {
        return await _repository.CountForUserAsync(userId);
    }

    public async Task<JournalEntry> Update(
        JournalEntry journalEntry,
        IEnumerable<Guid>? tagIds = null,
        IEnumerable<SystemTagAssignment>? systemTagAssignments = null)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        // Remove existing tags
        var existingTags = _context.JournalEntryTags
            .Where(jet => jet.JournalEntryId == journalEntry.Id)
            .ToList();
        _context.JournalEntryTags.RemoveRange(existingTags);

        // Add new tags if provided
        if (tagIds != null && tagIds.Any())
        {
            var tags = await _tagRepository.GetByIdsForUserAsync(tagIds, journalEntry.UserId);
            foreach (var tag in tags)
            {
                var journalEntryTag = new JournalEntryTag
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = journalEntry.Id,
                    TagId = tag.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.JournalEntryTags.Add(journalEntryTag);
            }
        }

        // Remove existing system tags
        var existingSystemTags = _context.JournalEntrySystemTags
            .Where(jest => jest.JournalEntryId == journalEntry.Id)
            .ToList();
        _context.JournalEntrySystemTags.RemoveRange(existingSystemTags);

        // Add new system tags if provided
        if (systemTagAssignments != null)
        {
            var assignments = systemTagAssignments
                .Where(a => a.SystemTagId != Guid.Empty)
                .GroupBy(a => a.SystemTagId)
                .Select(g => g.First())
                .ToList();

            if (assignments.Count > 0)
            {
                var ids = assignments.Select(a => a.SystemTagId).ToList();
                var existingIds = await _context.SystemTags
                    .AsNoTracking()
                    .Where(st => ids.Contains(st.Id))
                    .Select(st => st.Id)
                    .ToListAsync();

                var missing = ids.Except(existingIds).ToList();
                if (missing.Count > 0)
                {
                    throw new AppException("INVALID_SYSTEM_TAG", $"Unknown system tag(s): {string.Join(", ", missing)}", 400);
                }

                foreach (var assignment in assignments)
                {
                    var jest = new JournalEntrySystemTag
                    {
                        Id = Guid.NewGuid(),
                        JournalEntryId = journalEntry.Id,
                        SystemTagId = assignment.SystemTagId,
                        ResponsesJson = assignment.ResponsesJson,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.JournalEntrySystemTags.Add(jest);
                }
            }
        }

        journalEntry.UpdatedAt = DateTime.UtcNow;
        _repository.Update(journalEntry);
        await _unitOfWork.CommitTransactionAsync();
        return journalEntry;
    }

    public async Task<JournalEntry> Remove(JournalEntry journalEntry)
    {
        // Remove associated tags
        var existingTags = _context.JournalEntryTags
            .Where(jet => jet.JournalEntryId == journalEntry.Id)
            .ToList();
        _context.JournalEntryTags.RemoveRange(existingTags);

        // Remove associated system tags
        var existingSystemTags = _context.JournalEntrySystemTags
            .Where(jest => jest.JournalEntryId == journalEntry.Id)
            .ToList();
        _context.JournalEntrySystemTags.RemoveRange(existingSystemTags);

        _repository.Remove(journalEntry);
        await _unitOfWork.SaveChangesAsync();
        return journalEntry;
    }
}

public interface IJournalEntryService
{
    Task<JournalEntry> Add(
        JournalEntry journalEntry,
        IEnumerable<Guid>? tagIds = null,
        IEnumerable<SystemTagAssignment>? systemTagAssignments = null);
    Task<JournalEntry?> GetById(Guid id, string userId);
    Task<JournalEntry?> GetByIdWithTags(Guid id, string userId);
    Task<IEnumerable<JournalEntry>> GetAllForUser(string userId);
    Task<IEnumerable<JournalEntry>> GetPagedForUser(string userId, int skip, int take);
    Task<IEnumerable<JournalEntry>> GetRecentForUser(string userId, int count);
    Task<int> CountForUser(string userId);
    Task<JournalEntry> Update(
        JournalEntry journalEntry,
        IEnumerable<Guid>? tagIds = null,
        IEnumerable<SystemTagAssignment>? systemTagAssignments = null);
    Task<JournalEntry> Remove(JournalEntry journalEntry);
}

