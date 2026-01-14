using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

/// <summary>
/// Service for cleaning up invalid and stale device tokens
/// </summary>
public class DeviceTokenCleanupService : IDeviceTokenCleanupService
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeviceTokenCleanupService> _logger;
    private readonly IConfiguration _configuration;

    public DeviceTokenCleanupService(
        IDeviceTokenRepository deviceTokenRepository,
        IPushNotificationService pushNotificationService,
        IUnitOfWork unitOfWork,
        ILogger<DeviceTokenCleanupService> logger,
        IConfiguration configuration)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _pushNotificationService = pushNotificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<int> CleanupInvalidTokensAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = _configuration.GetValue<int>("PushNotifications:ValidationBatchSize", 100);
        var lastValidatedThreshold = DateTime.UtcNow.AddDays(-7); // Validate tokens not validated in last 7 days

        _logger.LogInformation("Starting proactive token cleanup. Batch size: {BatchSize}", batchSize);

        var totalMarkedInvalid = 0;

        try
        {
            // Get a batch of tokens to validate
            var tokensToValidate = await _deviceTokenRepository.GetTokensToValidateAsync(
                batchSize,
                lastValidatedThreshold,
                cancellationToken);

            if (!tokensToValidate.Any())
            {
                _logger.LogInformation("No tokens found that need validation");
                return 0;
            }

            _logger.LogInformation("Validating {Count} tokens", tokensToValidate.Count());

            // Test tokens by sending silent data-only notifications (these won't be visible to users)
            // Note: FCM doesn't have a dedicated validation API, so we test by attempting to send
            // a data-only notification. Invalid tokens will fail immediately.
            var tokenList = tokensToValidate.Select(t => t.Token).ToList();

            // Send notifications one by one for validation (batch may fail if any token is invalid)
            // Actually, we can use SendNotificationToMultipleAsync which handles invalid tokens gracefully
            var testResult = await _pushNotificationService.SendNotificationToMultipleAsync(
                tokenList,
                "silent_validation", // Title for validation (will be ignored in data-only mode)
                "silent_validation", // Body for validation (will be ignored in data-only mode)
                new Dictionary<string, string> { { "type", "validation_test" }, { "silent", "true" } },
                cancellationToken);

            // Mark invalid tokens
            if (testResult.InvalidTokens.Any())
            {
                var invalidCount = await _deviceTokenRepository.MarkTokensAsInvalidAsync(
                    testResult.InvalidTokens,
                    "Proactive validation failed",
                    cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                totalMarkedInvalid += invalidCount;
                _logger.LogInformation("Marked {Count} tokens as invalid during proactive validation", invalidCount);
            }

            // Update LastValidatedAt for successfully validated tokens
            var validatedTokenStrings = tokensToValidate
                .Where(t => !testResult.InvalidTokens.Contains(t.Token) && !testResult.FailedTokens.Contains(t.Token))
                .Select(t => t.Token)
                .ToList();

            if (validatedTokenStrings.Any())
            {
                // Load tokens with tracking enabled for updates
                var validatedTokensToUpdate = await _deviceTokenRepository
                    .GetTokensByTokenStringsWithTrackingAsync(validatedTokenStrings, cancellationToken);

                foreach (var token in validatedTokensToUpdate)
                {
                    token.LastValidatedAt = DateTime.UtcNow;
                    _deviceTokenRepository.Update(token);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated LastValidatedAt for {Count} successfully validated tokens", validatedTokensToUpdate.Count());
            }

            if (testResult.FailedTokens.Any())
            {
                _logger.LogWarning("Failed to validate {Count} tokens (non-fatal errors)", testResult.FailedTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during proactive token cleanup");
            throw;
        }

        return totalMarkedInvalid;
    }

    public async Task<int> RemoveStaleTokensAsync(CancellationToken cancellationToken = default)
    {
        var staleDays = _configuration.GetValue<int>("PushNotifications:StaleTokenDays", 90);
        var inactiveBefore = DateTime.UtcNow.AddDays(-staleDays);

        _logger.LogInformation("Starting stale token cleanup. Removing tokens inactive for more than {Days} days", staleDays);

        try
        {
            var staleTokens = await _deviceTokenRepository.GetStaleInactiveTokensAsync(
                inactiveBefore,
                cancellationToken);

            if (!staleTokens.Any())
            {
                _logger.LogInformation("No stale inactive tokens found");
                return 0;
            }

            var count = staleTokens.Count();
            foreach (var token in staleTokens)
            {
                _deviceTokenRepository.Remove(token);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} stale inactive tokens", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stale token cleanup");
            throw;
        }
    }
}

