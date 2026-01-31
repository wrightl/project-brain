namespace ProjectBrain.Domain.Repositories;

public interface IAchievementRepository : IRepository<Achievement, Guid>
{
    Task<List<Achievement>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<Achievement?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
}

