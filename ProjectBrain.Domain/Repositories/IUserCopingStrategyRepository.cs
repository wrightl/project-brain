namespace ProjectBrain.Domain.Repositories;

public interface IUserCopingStrategyRepository : IRepository<UserCopingStrategy, Guid>
{
    Task<List<UserCopingStrategy>> GetLibraryForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserCopingStrategy?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}

