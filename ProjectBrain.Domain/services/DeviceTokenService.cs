namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class DeviceTokenService : IDeviceTokenService
{
    private readonly IDeviceTokenRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeviceTokenService(IDeviceTokenRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeviceTokenRegistrationResult> RegisterOrUpdateAsync(
        string userId,
        string token,
        string? platform,
        string? deviceId,
        CancellationToken cancellationToken = default)
    {
        var existingToken = await _repository.GetByTokenAsync(token, cancellationToken);
        if (existingToken != null)
        {
            if (existingToken.UserId != userId)
            {
                return DeviceTokenRegistrationResult.Rejected("Device token is already registered to another account");
            }

            existingToken.Platform = platform;
            existingToken.DeviceId = deviceId;
            existingToken.LastUsedAt = DateTime.UtcNow;
            existingToken.IsActive = true;
            existingToken.InvalidReason = null;
            _repository.Update(existingToken);
        }
        else
        {
            _repository.Add(new DeviceToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                Platform = platform,
                DeviceId = deviceId,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DeviceTokenRegistrationResult.Success();
    }

    public async Task<IReadOnlyList<string>> GetActiveTokenStringsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _repository.GetActiveTokensByUserIdAsync(userId, cancellationToken);
        return tokens.Select(t => t.Token).ToList();
    }

    public async Task<bool> DeactivateTokenForUserAsync(
        string userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var deviceToken = await _repository.GetByTokenAsync(token, cancellationToken);
        if (deviceToken == null || deviceToken.UserId != userId)
        {
            return false;
        }

        deviceToken.IsActive = false;
        deviceToken.InvalidReason = "Removed by user";
        _repository.Update(deviceToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class DeviceTokenRegistrationResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static DeviceTokenRegistrationResult Success() => new() { Succeeded = true };

    public static DeviceTokenRegistrationResult Rejected(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

public interface IDeviceTokenService
{
    Task<DeviceTokenRegistrationResult> RegisterOrUpdateAsync(
        string userId,
        string token,
        string? platform,
        string? deviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetActiveTokenStringsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateTokenForUserAsync(
        string userId,
        string token,
        CancellationToken cancellationToken = default);
}
