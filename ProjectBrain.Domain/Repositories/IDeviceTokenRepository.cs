namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for DeviceToken entity with domain-specific queries
/// </summary>
public interface IDeviceTokenRepository : IRepository<DeviceToken, Guid>
{
    /// <summary>
    /// Gets a device token by its FCM token string
    /// </summary>
    Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active device tokens for a specific user
    /// </summary>
    Task<IEnumerable<DeviceToken>> GetActiveTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tokens that need validation (haven't been validated recently)
    /// </summary>
    Task<IEnumerable<DeviceToken>> GetTokensToValidateAsync(int batchSize, DateTime? lastValidatedBefore = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stale inactive tokens that can be removed
    /// </summary>
    Task<IEnumerable<DeviceToken>> GetStaleInactiveTokensAsync(DateTime inactiveBefore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks multiple tokens as invalid by their token strings
    /// </summary>
    Task<int> MarkTokensAsInvalidAsync(IEnumerable<string> tokens, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tokens by their token strings with tracking enabled (for updates)
    /// </summary>
    Task<IEnumerable<DeviceToken>> GetTokensByTokenStringsWithTrackingAsync(IEnumerable<string> tokens, CancellationToken cancellationToken = default);
}

