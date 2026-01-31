namespace ProjectBrain.Domain.Repositories;

public interface ISystemTagRepository : IRepository<SystemTag, Guid>
{
    Task<List<SystemTag>> GetAllWithFieldsAsync(CancellationToken cancellationToken = default);
    Task<List<SystemTag>> GetByIdsAsync(IEnumerable<Guid> systemTagIds, CancellationToken cancellationToken = default);
}

