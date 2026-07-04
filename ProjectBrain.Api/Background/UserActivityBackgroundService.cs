using System.Threading.Channels;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Background;

public interface IUserActivityQueue
{
    void Enqueue(string userId);
}

public class UserActivityBackgroundService : BackgroundService, IUserActivityQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<UserActivityBackgroundService> _logger;

    public UserActivityBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<UserActivityBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public void Enqueue(string userId)
    {
        _channel.Writer.TryWrite(userId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var userId in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var userActivityService = scope.ServiceProvider.GetRequiredService<IUserActivityService>();

            try
            {
                await userActivityService.RecordUserActivityAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record user activity for user {UserId}", userId);
            }
        }
    }
}
