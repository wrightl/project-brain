using System.Text.Json;
using Azure.Storage.Queues;

namespace ProjectBrain.Api.Background;

public sealed class AzureStorageChatPersistenceQueue : IChatPersistenceQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly QueueClient _queueClient;
    private readonly ILogger<AzureStorageChatPersistenceQueue> _logger;

    public AzureStorageChatPersistenceQueue(QueueServiceClient queueServiceClient, ILogger<AzureStorageChatPersistenceQueue> logger)
    {
        _logger = logger;
        _queueClient = queueServiceClient.GetQueueClient(ChatPersistenceConstants.QueueName);
    }

    public async Task<bool> TryEnqueueAsync(ChatPersistenceQueueMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            var json = JsonSerializer.Serialize(message, JsonOptions);
            await _queueClient.SendMessageAsync(json, cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue chat persistence message for conversation {ConversationId}", message.ConversationId);
            return false;
        }
    }
}
