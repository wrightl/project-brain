using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Hubs;
using ProjectBrain.Domain;

public class CoachMessageServices(
    ILogger<CoachMessageServices> logger,
    ICoachMessageService coachMessageService,
    IConnectionService connectionService,
    IIdentityService identityService,
    Storage storage,
    IConfiguration configuration,
    IHubContext<CoachMessageHub> hubContext)
{
    public ILogger<CoachMessageServices> Logger { get; } = logger;
    public ICoachMessageService CoachMessageService { get; } = coachMessageService;
    public IConnectionService ConnectionService { get; } = connectionService;
    public IIdentityService IdentityService { get; } = identityService;
    public Storage Storage { get; } = storage;
    public IConfiguration Configuration { get; } = configuration;
    public IHubContext<CoachMessageHub> HubContext { get; } = hubContext;
}

public static class CoachMessageEndpoints
{
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB
    private static readonly string[] AllowedMimeTypes = { "audio/m4a", "audio/mpeg", "audio/aac", "audio/wav", "audio/x-m4a", "audio/mp4" };
    private static readonly string[] AllowedExtensions = { ".m4a", ".mp3", ".aac", ".wav" };

    public static void MapCoachMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("coach-messages").RequireAuthorization();

        group.MapGet("/conversations", GetConversations).WithName("GetConversations");
        group.MapGet("/conversation/{connectionId:guid}", GetConversationMessages).WithName("GetCoachConversationMessages");
        group.MapPost("/", SendMessage).WithName("SendMessage");
        group.MapPost("/voice", SendVoiceMessage).WithName("SendVoiceMessage");
        group.MapPut("/{messageId}/delivered", MarkAsDelivered).WithName("MarkAsDelivered");
        group.MapPut("/{messageId}/read", MarkAsRead).WithName("MarkAsRead");
        group.MapPut("/conversation/{connectionId}/read", MarkConversationAsRead).WithName("MarkConversationAsRead");
        group.MapGet("/conversation/{connectionId}/search", SearchMessages).WithName("SearchMessages");
        group.MapDelete("/{messageId}", DeleteMessage).WithName("DeleteMessage");
        group.MapGet("/{messageId}/audio", GetMessageAudio).WithName("GetMessageAudio");
    }

    private static async Task<IResult> GetConversations(
        [AsParameters] CoachMessageServices services)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            // Is user a coach or user
            var isCoach = services.IdentityService.IsCoach;
            var conversations = await services.CoachMessageService.GetConversationsAsync(currentUserId, isCoach);

            var response = conversations.Select(c => new
            {
                connectionId = c.ConnectionId.ToString(),
                otherPersonName = c.OtherPersonName,
                lastMessageSnippet = c.LastMessageSnippet,
                lastMessageSenderName = c.LastMessageSenderName,
                lastMessageTimestamp = c.LastMessageTimestamp?.ToString("O"),
                unreadCount = c.UnreadCount
            }).ToList();

            // Get other people the person is connected to
            var connections = await services.ConnectionService.GetConnectionsAsync(currentUserId, isCoach);

            foreach (var connection in connections)
            {
                var otherPersonName = connection.Id == currentUserId ? "You" : connection.Id;

                if (response.Any(r => r.connectionId.Equals(connection.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                response.Add(new
                {
                    connectionId = connection.Id,
                    otherPersonName = isCoach ? connection.CoachName : connection.UserName,
                    lastMessageSnippet = string.Empty,
                    lastMessageSenderName = string.Empty,
                    lastMessageTimestamp = string.Empty,
                    unreadCount = 0
                });
            }
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error getting conversations for user {UserId}", currentUserId);
            return Results.Problem("An error occurred while fetching conversations.");
        }
    }

    private static async Task<IResult> GetConversationMessages(
        [AsParameters] CoachMessageServices services,
        Guid connectionId,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? beforeDate = null)
    {
        var currentUserId = services.IdentityService.UserId!;

        // Verify connection exists and is accepted
        var connection = await services.ConnectionService.GetByIdAsync(connectionId);

        if (connection == null || connection.Status != "accepted")
        {
            return Results.BadRequest(new
            {
                error = new
                {
                    code = "NO_CONNECTION",
                    message = "No active connection between these users"
                }
            });
        }

        // Verify user has access to this conversation
        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            return Results.Forbid();
        }

        try
        {
            var messages = await services.CoachMessageService.GetConversationMessagesAsync(
                connectionId,
                pageSize,
                beforeDate);

            // TODO: Remove the userId, coachId, senderId, and sender properties from the response
            var response = messages.Select(m => new
            {
                id = m.Id.ToString(),
                // userId = m.UserId,
                // coachId = m.CoachId,
                connectionId = m.ConnectionId.ToString(),
                // senderId = m.SenderId,
                messageType = m.MessageType,
                content = m.Content,
                voiceNoteUrl = m.VoiceNoteUrl,
                voiceNoteFileName = m.VoiceNoteFileName,
                status = m.Status,
                deliveredAt = m.DeliveredAt?.ToString("O"),
                readAt = m.ReadAt?.ToString("O"),
                createdAt = m.CreatedAt.ToString("O"),
                sender = m.Sender != null ? new
                {
                    // id = m.Sender.Id,
                    fullName = m.Sender.FullName,
                    email = m.Sender.Email
                } : null
            }).ToList();

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving conversation messages");
            return Results.Problem("An error occurred while retrieving messages.");
        }
    }

    private static async Task<IResult> SendMessage(
        [AsParameters] CoachMessageServices services,
        SendMessageRequest request)
    {
        var currentUserId = services.IdentityService.UserId!;

        var connectionGuid = Guid.Parse(request.ConnectionId);
        var connection = await services.ConnectionService.GetByIdAsync(connectionGuid);
        if (connection == null)
        {
            return Results.BadRequest(new
            {
                error = new
                {
                    code = "NO_CONNECTION",
                    message = "No active connection between these users"
                }
            });
        }

        // Determine if current user is the user or coach
        var isUser = currentUserId == connection.UserId;
        var isCoach = currentUserId == connection.CoachId;

        if (!isUser && !isCoach)
        {
            return Results.Forbid();
        }

        try
        {
            var message = new CoachMessage
            {
                UserId = connection.UserId,
                CoachId = connection.CoachId,
                ConnectionId = connection.Id,
                SenderId = currentUserId,
                MessageType = "text",
                Content = request.Content,
                Status = "sent",
                CreatedAt = DateTime.UtcNow
            };

            var savedMessage = await services.CoachMessageService.Add(message);

            // Load sender information
            var messageWithSender = await services.CoachMessageService.GetById(savedMessage.Id);

            var response = new
            {
                id = messageWithSender!.Id.ToString(),
                userId = messageWithSender.UserId,
                coachId = messageWithSender.CoachId,
                connectionId = messageWithSender.ConnectionId.ToString(),
                senderId = messageWithSender.SenderId,
                messageType = messageWithSender.MessageType,
                content = messageWithSender.Content,
                voiceNoteUrl = messageWithSender.VoiceNoteUrl,
                voiceNoteFileName = messageWithSender.VoiceNoteFileName,
                status = messageWithSender.Status,
                deliveredAt = messageWithSender.DeliveredAt?.ToString("O"),
                readAt = messageWithSender.ReadAt?.ToString("O"),
                createdAt = messageWithSender.CreatedAt.ToString("O"),
                sender = messageWithSender.Sender != null ? new
                {
                    id = messageWithSender.Sender.Id,
                    fullName = messageWithSender.Sender.FullName,
                    email = messageWithSender.Sender.Email
                } : null
            };

            // Notify other party via SignalR
            var recipientId = currentUserId == connection.UserId ? connection.CoachId : connection.UserId;
            await services.HubContext.Clients.Group($"user_{recipientId}").SendAsync("NewMessage", response);

            return Results.Created($"/coach-messages/{savedMessage.Id}", response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error sending message");
            return Results.Problem("An error occurred while sending the message.");
        }
    }

    private static async Task<IResult> SendVoiceMessage(
        [AsParameters] CoachMessageServices services,
        HttpRequest request)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var form = await request.ReadFormAsync();

            if (form.Files.Count == 0)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "MISSING_FILE",
                        message = "No file provided in upload request"
                    }
                });
            }

            var file = form.Files[0];
            var connectionId = form["connectionId"].ToString();

            var connectionGuid = Guid.Parse(connectionId);
            var connection = await services.ConnectionService.GetByIdAsync(connectionGuid);
            if (connection == null)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "NO_CONNECTION",
                        message = "No active connection between these users"
                    }
                });
            }

            // Determine if current user is the user or coach
            var isUser = currentUserId == connection.UserId;
            var isCoach = currentUserId == connection.CoachId;

            if (!isUser && !isCoach)
            {
                return Results.Forbid();
            }

            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "MISSING_FILE",
                        message = "File is empty"
                    }
                });
            }

            if (file.Length > MaxFileSize)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "FILE_TOO_LARGE",
                        message = $"File size exceeds maximum of {MaxFileSize / (1024 * 1024)} MB"
                    }
                });
            }

            var contentType = file.ContentType;
            var fileName = file.FileName ?? "voice_note.m4a";
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension) && !AllowedMimeTypes.Contains(contentType))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_FILE_FORMAT",
                        message = $"File format not supported. Allowed formats: {string.Join(", ", AllowedExtensions)}"
                    }
                });
            }

            // Generate unique filename
            var messageId = Guid.NewGuid();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sanitizedFileName = SanitizeFileName(fileName);
            var uniqueFileName = $"{messageId}_{timestamp}{extension}";

            // Upload file to storage
            var storageFileName = $"{messageId}{extension}";
            await using var fileStream = file.OpenReadStream();
            var metadata = new Dictionary<string, string>
            {
                ["mimeType"] = contentType ?? "audio/m4a"
            };
            var blobPath = await services.Storage.UploadFile(
                fileStream,
                storageFileName,
                messageId.ToString(),
                currentUserId,
                skipIndexing: true,
                metadata: metadata,
                parentFolder: "coach-messages");

            // Build audio URL
            var baseUrl = services.Configuration["ApiBaseUrl"] ??
                         $"{request.Scheme}://{request.Host}";
            var audioUrl = $"{baseUrl}/coach-messages/{messageId}/audio";

            // Create message record
            var message = new CoachMessage
            {
                Id = messageId,
                UserId = connection.UserId,
                CoachId = connection.CoachId,
                ConnectionId = connection.Id,
                SenderId = currentUserId,
                MessageType = "voice",
                Content = sanitizedFileName, // Store filename as content
                VoiceNoteUrl = audioUrl,
                VoiceNoteFileName = sanitizedFileName,
                Status = "sent",
                CreatedAt = DateTime.UtcNow
            };

            var savedMessage = await services.CoachMessageService.Add(message);

            // Load sender information
            var messageWithSender = await services.CoachMessageService.GetById(savedMessage.Id);

            var response = new
            {
                id = messageWithSender!.Id.ToString(),
                userId = messageWithSender.UserId,
                coachId = messageWithSender.CoachId,
                connectionId = messageWithSender.ConnectionId.ToString(),
                senderId = messageWithSender.SenderId,
                messageType = messageWithSender.MessageType,
                content = messageWithSender.Content,
                voiceNoteUrl = messageWithSender.VoiceNoteUrl,
                voiceNoteFileName = messageWithSender.VoiceNoteFileName,
                status = messageWithSender.Status,
                deliveredAt = messageWithSender.DeliveredAt?.ToString("O"),
                readAt = messageWithSender.ReadAt?.ToString("O"),
                createdAt = messageWithSender.CreatedAt.ToString("O"),
                sender = messageWithSender.Sender != null ? new
                {
                    id = messageWithSender.Sender.Id,
                    fullName = messageWithSender.Sender.FullName,
                    email = messageWithSender.Sender.Email
                } : null
            };

            // Notify other party via SignalR
            var recipientId = currentUserId == connection.UserId ? connection.CoachId : connection.UserId;
            await services.HubContext.Clients.Group($"user_{recipientId}").SendAsync("NewMessage", response);

            return Results.Created($"/coach-messages/{savedMessage.Id}", response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error sending voice message");
            return Results.Problem("An error occurred while sending the voice message.");
        }
    }

    private static async Task<IResult> MarkAsDelivered(
        [AsParameters] CoachMessageServices services,
        Guid messageId)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var message = await services.CoachMessageService.GetById(messageId);
            if (message == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "MESSAGE_NOT_FOUND",
                        message = "Message not found"
                    }
                });
            }

            // Verify user is the recipient
            if (message.SenderId == currentUserId)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_OPERATION",
                        message = "Cannot mark your own message as delivered"
                    }
                });
            }

            var success = await services.CoachMessageService.MarkAsDeliveredAsync(messageId, currentUserId);
            if (!success)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "UPDATE_FAILED",
                        message = "Failed to update message status"
                    }
                });
            }

            // Fetch updated message with sender information
            var updatedMessage = await services.CoachMessageService.GetById(messageId);
            if (updatedMessage != null)
            {
                // Format message object for SignalR
                var messageUpdate = new
                {
                    id = updatedMessage.Id.ToString(),
                    userId = updatedMessage.UserId,
                    coachId = updatedMessage.CoachId,
                    connectionId = updatedMessage.ConnectionId.ToString(),
                    senderId = updatedMessage.SenderId,
                    messageType = updatedMessage.MessageType,
                    content = updatedMessage.Content,
                    voiceNoteUrl = updatedMessage.VoiceNoteUrl,
                    voiceNoteFileName = updatedMessage.VoiceNoteFileName,
                    status = updatedMessage.Status,
                    deliveredAt = updatedMessage.DeliveredAt?.ToString("O"),
                    readAt = updatedMessage.ReadAt?.ToString("O"),
                    createdAt = updatedMessage.CreatedAt.ToString("O"),
                    sender = updatedMessage.Sender != null ? new
                    {
                        id = updatedMessage.Sender.Id,
                        fullName = updatedMessage.Sender.FullName,
                        email = updatedMessage.Sender.Email
                    } : null
                };

                // Notify sender via SignalR
                try
                {
                    await services.HubContext.Clients.Group($"user_{message.SenderId}").SendAsync("MessageDelivered", messageUpdate);
                }
                catch (Exception signalrEx)
                {
                    services.Logger.LogError(signalrEx, "Error sending SignalR notification for message delivery");
                    // Continue even if SignalR fails - don't fail the API request
                }
            }

            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error marking message as delivered");
            return Results.Problem("An error occurred while updating message status.");
        }
    }

    private static async Task<IResult> MarkAsRead(
        [AsParameters] CoachMessageServices services,
        Guid messageId)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var message = await services.CoachMessageService.GetById(messageId);
            if (message == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "MESSAGE_NOT_FOUND",
                        message = "Message not found"
                    }
                });
            }

            // Verify user is the recipient
            if (message.SenderId == currentUserId)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_OPERATION",
                        message = "Cannot mark your own message as read"
                    }
                });
            }

            var success = await services.CoachMessageService.MarkAsReadAsync(messageId, currentUserId);
            if (!success)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "UPDATE_FAILED",
                        message = "Failed to update message status"
                    }
                });
            }

            // Fetch updated message with sender information
            var updatedMessage = await services.CoachMessageService.GetById(messageId);
            if (updatedMessage != null)
            {
                // Format message object for SignalR
                var messageUpdate = new
                {
                    id = updatedMessage.Id.ToString(),
                    userId = updatedMessage.UserId,
                    coachId = updatedMessage.CoachId,
                    connectionId = updatedMessage.ConnectionId.ToString(),
                    senderId = updatedMessage.SenderId,
                    messageType = updatedMessage.MessageType,
                    content = updatedMessage.Content,
                    voiceNoteUrl = updatedMessage.VoiceNoteUrl,
                    voiceNoteFileName = updatedMessage.VoiceNoteFileName,
                    status = updatedMessage.Status,
                    deliveredAt = updatedMessage.DeliveredAt?.ToString("O"),
                    readAt = updatedMessage.ReadAt?.ToString("O"),
                    createdAt = updatedMessage.CreatedAt.ToString("O"),
                    sender = updatedMessage.Sender != null ? new
                    {
                        id = updatedMessage.Sender.Id,
                        fullName = updatedMessage.Sender.FullName,
                        email = updatedMessage.Sender.Email
                    } : null
                };

                // Notify sender via SignalR
                try
                {
                    await services.HubContext.Clients.Group($"user_{message.SenderId}").SendAsync("MessageRead", messageUpdate);
                }
                catch (Exception signalrEx)
                {
                    services.Logger.LogError(signalrEx, "Error sending SignalR notification for message read");
                    // Continue even if SignalR fails - don't fail the API request
                }
            }

            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error marking message as read");
            return Results.Problem("An error occurred while updating message status.");
        }
    }

    private static async Task<IResult> MarkConversationAsRead(
        [AsParameters] CoachMessageServices services,
        string connectionId)
    {
        var currentUserId = services.IdentityService.UserId!;

        var connectionGuid = Guid.Parse(connectionId);
        var connection = await services.ConnectionService.GetByIdAsync(connectionGuid);
        if (connection == null)
        {
            return Results.BadRequest(new
            {
                error = new
                {
                    code = "NO_CONNECTION",
                    message = "No active connection between these users"
                }
            });
        }

        // Verify user has access to this conversation
        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            return Results.Forbid();
        }

        try
        {
            await services.CoachMessageService.MarkConversationAsReadAsync(connectionGuid, currentUserId);
            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error marking conversation as read");
            return Results.Problem("An error occurred while updating conversation status.");
        }
    }

    private static async Task<IResult> SearchMessages(
        [AsParameters] CoachMessageServices services,
        string connectionId,
        [FromQuery] string searchTerm)
    {
        var currentUserId = services.IdentityService.UserId!;

        var connectionGuid = Guid.Parse(connectionId);
        var connection = await services.ConnectionService.GetByIdAsync(connectionGuid);
        if (connection == null)
        {
            return Results.BadRequest(new
            {
                error = new
                {
                    code = "NO_CONNECTION",
                    message = "No active connection between these users"
                }
            });
        }

        // Verify user has access to this conversation
        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Results.BadRequest(new
            {
                error = new
                {
                    code = "MISSING_SEARCH_TERM",
                    message = "Search term is required"
                }
            });
        }

        try
        {
            var messages = await services.CoachMessageService.SearchMessagesAsync(connectionGuid, searchTerm);

            var response = messages.Select(m => new
            {
                id = m.Id.ToString(),
                userId = m.UserId,
                coachId = m.CoachId,
                connectionId = m.ConnectionId.ToString(),
                senderId = m.SenderId,
                messageType = m.MessageType,
                content = m.Content,
                voiceNoteUrl = m.VoiceNoteUrl,
                voiceNoteFileName = m.VoiceNoteFileName,
                status = m.Status,
                deliveredAt = m.DeliveredAt?.ToString("O"),
                readAt = m.ReadAt?.ToString("O"),
                createdAt = m.CreatedAt.ToString("O"),
                sender = m.Sender != null ? new
                {
                    id = m.Sender.Id,
                    fullName = m.Sender.FullName,
                    email = m.Sender.Email
                } : null
            }).ToList();

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error searching messages");
            return Results.Problem("An error occurred while searching messages.");
        }
    }

    private static async Task<IResult> DeleteMessage(
        [AsParameters] CoachMessageServices services,
        Guid messageId)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var message = await services.CoachMessageService.GetById(messageId);
            if (message == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "MESSAGE_NOT_FOUND",
                        message = "Message not found"
                    }
                });
            }

            // Verify user is the sender
            if (message.SenderId != currentUserId)
            {
                return Results.Forbid();
            }

            var success = await services.CoachMessageService.Delete(messageId);
            if (!success)
            {
                return Results.Problem("Failed to delete message.");
            }

            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error deleting message");
            return Results.Problem("An error occurred while deleting the message.");
        }
    }

    private static async Task<IResult> GetMessageAudio(
        [AsParameters] CoachMessageServices services,
        Guid messageId)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var message = await services.CoachMessageService.GetById(messageId);
            if (message == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "MESSAGE_NOT_FOUND",
                        message = "Message not found"
                    }
                });
            }

            // Verify user has access to this message
            if (message.UserId != currentUserId && message.CoachId != currentUserId)
            {
                return Results.Forbid();
            }

            // Only voice messages have audio
            if (message.MessageType != "voice" || string.IsNullOrEmpty(message.VoiceNoteUrl))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "NOT_VOICE_MESSAGE",
                        message = "Message is not a voice message"
                    }
                });
            }

            // Extract the blob path from the stored location
            // The blob path should be in the format: coach-messages/{userId}/{messageId}.{ext}
            var blobPath = $"coach-messages/{message.SenderId}/{messageId}";
            var extension = Path.GetExtension(message.VoiceNoteFileName ?? ".m4a");
            blobPath += extension;

            // Get the audio file from storage
            var audioStream = await services.Storage.GetFile(blobPath);
            if (audioStream == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "AUDIO_NOT_FOUND",
                        message = "Audio file not found"
                    }
                });
            }

            var contentType = message.VoiceNoteFileName?.ToLowerInvariant() switch
            {
                var fn when fn?.EndsWith(".m4a") == true => "audio/m4a",
                var fn when fn?.EndsWith(".mp3") == true => "audio/mpeg",
                var fn when fn?.EndsWith(".aac") == true => "audio/aac",
                var fn when fn?.EndsWith(".wav") == true => "audio/wav",
                _ => "audio/m4a"
            };

            return Results.File(audioStream, contentType);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving message audio");
            return Results.Problem("An error occurred while retrieving the audio.");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "voice_note.m4a";

        // Remove path separators and other dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Limit length
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            sanitized = sanitized[..(200 - extension.Length)] + extension;
        }

        return sanitized;
    }
}

public class SendMessageRequest
{
    public required string ConnectionId { get; init; }
    public required string Content { get; init; }
}

