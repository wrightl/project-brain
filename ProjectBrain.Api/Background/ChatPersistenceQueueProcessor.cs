using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.DependencyInjection;
using ProjectBrain.Domain;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Api.Background;

public sealed class ChatPersistenceQueueProcessor : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ILogger<ChatPersistenceQueueProcessor> _logger;

    public ChatPersistenceQueueProcessor(
        IServiceScopeFactory scopeFactory,
        QueueServiceClient queueServiceClient,
        ILogger<ChatPersistenceQueueProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _queueServiceClient = queueServiceClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = _queueServiceClient.GetQueueClient(ChatPersistenceConstants.QueueName);
        var queueReady = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!queueReady)
                {
                    await client.CreateIfNotExistsAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
                    queueReady = true;
                }

                var response = await client.ReceiveMessagesAsync(
                        maxMessages: 5,
                        visibilityTimeout: TimeSpan.FromSeconds(90),
                        cancellationToken: stoppingToken)
                    .ConfigureAwait(false);

                if (response.Value.Length == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                    continue;
                }

                foreach (var msg in response.Value)
                    await ProcessMessageAsync(client, msg, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                queueReady = false;
                _logger.LogError(ex, "Chat persistence queue poll failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessMessageAsync(QueueClient client, QueueMessage message, CancellationToken ct)
    {
        ChatPersistenceQueueMessage? dto;
        try
        {
            var text = message.Body.ToString();
            dto = JsonSerializer.Deserialize<ChatPersistenceQueueMessage>(text, JsonOptions);
            if (dto is null || string.IsNullOrWhiteSpace(dto.UserId))
                throw new InvalidOperationException("Invalid message body");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid chat persistence queue message. DequeueCount={Count}", message.DequeueCount);
            if (message.DequeueCount >= ChatPersistenceConstants.MaxDequeueAttemptsBeforePoison)
            {
                await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken: ct).ConfigureAwait(false);
            }

            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
            var usage = scope.ServiceProvider.GetRequiredService<IUsageTrackingService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await ChatPersistenceHelper.PersistSynchronouslyAsync(
                    chatService,
                    usage,
                    unitOfWork,
                    dto.ConversationId,
                    dto.UserId,
                    dto.UserContent,
                    dto.AssistantContent)
                .ConfigureAwait(false);

            await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist chat from queue for conversation {ConversationId}", dto.ConversationId);
            if (message.DequeueCount >= ChatPersistenceConstants.MaxDequeueAttemptsBeforePoison)
            {
                _logger.LogCritical(
                    "Giving up on chat persistence queue message after max attempts. Deleting. MessageId={MessageId}",
                    message.MessageId);
                await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken: ct).ConfigureAwait(false);
            }
        }
    }
}
