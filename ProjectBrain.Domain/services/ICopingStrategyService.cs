namespace ProjectBrain.Domain;

public interface ICopingStrategyService
{
    Task<List<UserCopingStrategy>> GetLibraryAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserCopingStrategy> CreateAsync(string userId, string title, string description, string? iconKey, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<UserCopingStrategy?> UpdateRatingAsync(string userId, Guid id, int rating, CancellationToken cancellationToken = default);
}

