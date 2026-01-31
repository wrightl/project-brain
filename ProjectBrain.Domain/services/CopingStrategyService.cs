namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class CopingStrategyService : ICopingStrategyService
{
    private readonly IUserCopingStrategyRepository _userCopingStrategyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CopingStrategyService(
        IUserCopingStrategyRepository userCopingStrategyRepository,
        IUnitOfWork unitOfWork)
    {
        _userCopingStrategyRepository = userCopingStrategyRepository ?? throw new ArgumentNullException(nameof(userCopingStrategyRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<List<UserCopingStrategy>> GetLibraryAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _userCopingStrategyRepository.GetLibraryForUserAsync(userId, cancellationToken);
    }

    public async Task<UserCopingStrategy> CreateAsync(
        string userId,
        string title,
        string description,
        string? iconKey,
        CancellationToken cancellationToken = default)
    {
        var strategy = new UserCopingStrategy
        {
            UserId = userId,
            Title = title,
            Description = description,
            IconKey = iconKey,
            SavedAt = DateTime.UtcNow,
        };

        _userCopingStrategyRepository.Add(strategy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return strategy;
    }

    public async Task<bool> DeleteAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _userCopingStrategyRepository.GetByIdForUserAsync(id, userId, cancellationToken);
        if (existing == null) return false;

        // Need tracked entity for deletion
        var tracked = await _userCopingStrategyRepository.GetByIdAsync(existing.Id, cancellationToken);
        if (tracked == null) return false;

        _userCopingStrategyRepository.Remove(tracked);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

