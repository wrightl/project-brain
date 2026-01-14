using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

/// <summary>
/// Service for sending push notifications via Firebase Cloud Messaging
/// </summary>
public class PushNotificationService : IPushNotificationService
{
    private readonly ILogger<PushNotificationService> _logger;
    private readonly FirebaseMessaging _messaging;
    private readonly IDeviceTokenRepository? _deviceTokenRepository;
    private readonly IUnitOfWork? _unitOfWork;

    public PushNotificationService(
        IConfiguration configuration,
        ILogger<PushNotificationService> logger,
        IDeviceTokenRepository? deviceTokenRepository = null,
        IUnitOfWork? unitOfWork = null)
    {
        _logger = logger;
        _deviceTokenRepository = deviceTokenRepository;
        _unitOfWork = unitOfWork;

        // Initialize Firebase Admin SDK if not already initialized
        if (FirebaseApp.DefaultInstance == null)
        {
            var firebaseCredentials = configuration["Firebase:CredentialsJson"];
            if (string.IsNullOrEmpty(firebaseCredentials))
            {
                throw new InvalidOperationException("Firebase:CredentialsJson is not configured");
            }

            try
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromJson(firebaseCredentials)
                });
                _logger.LogInformation("Firebase Admin SDK initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
                throw;
            }
        }

        _messaging = FirebaseMessaging.DefaultInstance;
    }

    public async Task<PushNotificationSendResult> SendNotificationAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var result = new PushNotificationSendResult();

        try
        {
            var message = new Message
            {
                Token = deviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                ),
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Sound = "default",
                        Badge = 1
                    }
                },
                Android = new AndroidConfig
                {
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ChannelId = "default"
                    },
                    Priority = Priority.High
                }
            };

            var response = await _messaging.SendAsync(message, cancellationToken);
            result.Success = true;
            result.MessageId = response;
            result.SuccessCount = 1;
            _logger.LogInformation("Successfully sent push notification. Message ID: {MessageId}", response);
        }
        catch (FirebaseMessagingException ex)
        {
            result.Success = false;
            _logger.LogError(ex, "Error sending push notification to token {Token}", deviceToken);

            // Check for invalid token errors
            if (ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                ex.MessagingErrorCode == MessagingErrorCode.SenderIdMismatch)
            {
                result.InvalidTokens.Add(deviceToken);
                _logger.LogWarning("Invalid or unregistered device token: {Token}, Error: {Error}", deviceToken, ex.MessagingErrorCode);
                await MarkTokenAsInvalidAsync(deviceToken, ex.MessagingErrorCode.ToString(), cancellationToken);
            }
            else
            {
                result.FailedTokens.Add(deviceToken);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.FailedTokens.Add(deviceToken);
            _logger.LogError(ex, "Unexpected error sending push notification");
        }

        return result;
    }

    public async Task<PushNotificationSendResult> SendNotificationToMultipleAsync(
        List<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var result = new PushNotificationSendResult();

        if (deviceTokens == null || !deviceTokens.Any())
        {
            return result;
        }

        try
        {
            // FCM allows up to 500 tokens per batch
            var batches = deviceTokens
                .Chunk(500)
                .Select(batch => batch.ToList())
                .ToList();

            foreach (var batch in batches)
            {
                var message = new MulticastMessage
                {
                    Tokens = batch,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data?.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value
                    ),
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default",
                            Badge = 1
                        }
                    },
                    Android = new AndroidConfig
                    {
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "default"
                        },
                        Priority = Priority.High
                    }
                };

                var response = await _messaging.SendMulticastAsync(message, cancellationToken);

                result.SuccessCount += response.SuccessCount;
                result.FailureCount += response.FailureCount;

                if (response.FailureCount > 0)
                {
                    for (var i = 0; i < response.Responses.Count; i++)
                    {
                        var batchResponse = response.Responses[i];
                        if (!batchResponse.IsSuccess)
                        {
                            var token = batch[i];
                            var exception = batchResponse.Exception;

                            if (exception is FirebaseMessagingException fcmEx)
                            {
                                // Check for invalid token errors
                                if (fcmEx.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                                    fcmEx.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                                    fcmEx.MessagingErrorCode == MessagingErrorCode.SenderIdMismatch)
                                {
                                    result.InvalidTokens.Add(token);
                                    _logger.LogWarning("Invalid or unregistered device token: {Token}, Error: {Error}", token, fcmEx.MessagingErrorCode);
                                    await MarkTokenAsInvalidAsync(token, fcmEx.MessagingErrorCode.ToString(), cancellationToken);
                                }
                                else
                                {
                                    result.FailedTokens.Add(token);
                                }
                            }
                            else
                            {
                                result.FailedTokens.Add(token);
                            }

                            _logger.LogWarning("Failed to send to token {Token}: {Error}",
                                token, exception?.Message ?? "Unknown error");
                        }
                    }
                }

                _logger.LogInformation("Sent {SuccessCount}/{TotalCount} notifications in batch",
                    response.SuccessCount, response.Responses.Count);
            }

            result.Success = result.FailureCount == 0;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.FailedTokens.AddRange(deviceTokens);
            _logger.LogError(ex, "Error sending multicast push notification");
        }

        return result;
    }

    public async Task<PushNotificationSendResult> SendNotificationToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var result = new PushNotificationSendResult();

        try
        {
            var message = new Message
            {
                Topic = topic,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                ),
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Sound = "default",
                        Badge = 1
                    }
                },
                Android = new AndroidConfig
                {
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ChannelId = "default"
                    },
                    Priority = Priority.High
                }
            };

            var response = await _messaging.SendAsync(message, cancellationToken);
            result.Success = true;
            result.MessageId = response;
            result.SuccessCount = 1;
            _logger.LogInformation("Successfully sent push notification to topic {Topic}. Message ID: {MessageId}",
                topic, response);
        }
        catch (Exception ex)
        {
            result.Success = false;
            _logger.LogError(ex, "Error sending push notification to topic {Topic}", topic);
        }

        return result;
    }

    /// <summary>
    /// Marks a token as invalid in the repository (reactive cleanup)
    /// </summary>
    private async Task MarkTokenAsInvalidAsync(string token, string reason, CancellationToken cancellationToken)
    {
        if (_deviceTokenRepository == null || _unitOfWork == null)
        {
            return; // Repository not available, skip cleanup
        }

        try
        {
            await _deviceTokenRepository.MarkTokensAsInvalidAsync(new[] { token }, reason, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Marked token as invalid: {Token}, Reason: {Reason}", token, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark token as invalid: {Token}", token);
            // Don't throw - cleanup failure shouldn't break notification sending
        }
    }
}

