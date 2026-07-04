namespace ProjectBrain.Domain.Repositories;

public interface IUserErasureRepository
{
    Task DeleteRelationalDataAsync(string userId, CancellationToken cancellationToken = default);
}
