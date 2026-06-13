namespace ProjectBrain.Domain.Repositories;

public interface ICoachSpecialismOptionRepository
{
    Task<List<string>> GetActiveNamesAsync(CancellationToken cancellationToken = default);
}
