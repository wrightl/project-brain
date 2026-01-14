
/// <summary>
/// Service interface for cleaning up invalid and stale device tokens
/// </summary>
public interface IDeviceTokenCleanupService
{
    /// <summary>
    /// Validates and marks invalid tokens as inactive
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens marked as invalid</returns>
    Task<int> CleanupInvalidTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes stale inactive tokens that are older than the configured threshold
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens removed</returns>
    Task<int> RemoveStaleTokensAsync(CancellationToken cancellationToken = default);
}

