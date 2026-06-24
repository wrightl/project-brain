namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class UserFactService : IUserFactService
{
    private readonly IUserFactRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserFactService(IUserFactRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserFact?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdForUserAsync(id, userId, cancellationToken);
    }

    public async Task<UserFact?> GetByContentHashAsync(string userId, string contentHash, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByContentHashAsync(userId, contentHash, cancellationToken);
    }

    public async Task<UserFact> AddAsync(UserFact fact, CancellationToken cancellationToken = default)
    {
        _repository.Add(fact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return fact;
    }

    public async Task UpdateAsync(UserFact fact, CancellationToken cancellationToken = default)
    {
        fact.UpdatedAt = DateTime.UtcNow;
        _repository.Update(fact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SupersedeAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var tracked = await _repository.GetByIdAsync(id, cancellationToken);
        if (tracked is null || tracked.UserId != userId)
        {
            return false;
        }

        tracked.Status = MemoryStatuses.Superseded;
        tracked.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserMemoryListDto> ListForUserAsync(string userId, bool includeProvisional, CancellationToken cancellationToken = default)
    {
        var statuses = includeProvisional
            ? new[] { MemoryStatuses.Active, MemoryStatuses.Provisional }
            : new[] { MemoryStatuses.Active };

        var facts = await _repository.GetForUserByStatusesAsync(userId, statuses, cancellationToken);
        return new UserMemoryListDto
        {
            Facts = facts.Select(f => new UserFactDto
            {
                Id = f.Id,
                Content = f.Content,
                Category = f.Category,
                Status = f.Status,
                CreatedAt = f.CreatedAt,
                IsPinned = f.PinnedAt is not null
            }).ToList()
        };
    }

    public Task TouchRetrievedAsync(IReadOnlyList<Guid> factIds, CancellationToken cancellationToken = default)
    {
        return _repository.TouchRetrievedAsync(factIds, cancellationToken);
    }

    public async Task<bool> PinAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var fact = await _repository.GetByIdAsync(id, cancellationToken);
        if (fact is null || fact.UserId != userId || fact.Status is MemoryStatuses.Superseded or MemoryStatuses.Rejected)
        {
            return false;
        }

        fact.PinnedAt = DateTime.UtcNow;
        fact.ExpiresAt = null;
        if (fact.Status == MemoryStatuses.Provisional)
        {
            fact.Status = MemoryStatuses.Active;
        }

        fact.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnpinAsync(string userId, Guid id, int activeTtlDays, CancellationToken cancellationToken = default)
    {
        var fact = await _repository.GetByIdAsync(id, cancellationToken);
        if (fact is null || fact.UserId != userId || fact.PinnedAt is null)
        {
            return false;
        }

        fact.PinnedAt = null;
        fact.ExpiresAt = DateTime.UtcNow.AddDays(activeTtlDays);
        fact.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public interface IUserFactService
{
    Task<UserFact?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<UserFact?> GetByContentHashAsync(string userId, string contentHash, CancellationToken cancellationToken = default);
    Task<UserFact> AddAsync(UserFact fact, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserFact fact, CancellationToken cancellationToken = default);
    Task<bool> SupersedeAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<UserMemoryListDto> ListForUserAsync(string userId, bool includeProvisional, CancellationToken cancellationToken = default);
    Task TouchRetrievedAsync(IReadOnlyList<Guid> factIds, CancellationToken cancellationToken = default);
    Task<bool> PinAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> UnpinAsync(string userId, Guid id, int activeTtlDays, CancellationToken cancellationToken = default);
}
