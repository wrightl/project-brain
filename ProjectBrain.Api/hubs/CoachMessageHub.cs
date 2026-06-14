using Microsoft.AspNetCore.SignalR;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Hubs;

public class CoachMessageHub : Hub
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<CoachMessageHub> _logger;
    private readonly IConnectionService _connectionService;
    public CoachMessageHub(
        IIdentityService identityService,
        IConnectionService connectionService,
        ILogger<CoachMessageHub> logger)
    {
        _identityService = identityService;
        _connectionService = connectionService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to message hub with connection {ConnectionId}", userId, Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from message hub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(string connectionId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == null)
        {
            _logger.LogWarning("Unauthorized attempt to join conversation");
            return;
        }

        if (!Guid.TryParse(connectionId, out var connectionGuid))
        {
            _logger.LogWarning("Invalid connection id {ConnectionId}", connectionId);
            return;
        }

        var connection = await _connectionService.GetByIdAsync(connectionGuid);
        if (connection == null)
        {
            _logger.LogWarning("Connection {ConnectionId} not found", connectionId);
            return;
        }

        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to join conversation {ConnectionId} without access",
                currentUserId, connectionId);
            return;
        }

        var groupName = GetConversationGroupName(connectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} joined conversation group {GroupName}", currentUserId, groupName);
    }

    public async Task LeaveConversation(string connectionId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == null)
        {
            _logger.LogWarning("Unauthorized attempt to leave conversation");
            return;
        }

        if (!Guid.TryParse(connectionId, out var connectionGuid))
        {
            _logger.LogWarning("Invalid connection id {ConnectionId}", connectionId);
            return;
        }

        var connection = await _connectionService.GetByIdAsync(connectionGuid);
        if (connection == null)
        {
            _logger.LogWarning("Connection {ConnectionId} not found", connectionId);
            return;
        }

        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to leave conversation {ConnectionId} without access",
                currentUserId, connectionId);
            return;
        }

        _logger.LogInformation("User {UserId} leaving conversation {ConnectionId}", currentUserId, connectionId);
        var groupName = GetConversationGroupName(connectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User left conversation group {GroupName}", groupName);
    }

    public async Task SendTypingIndicator(string connectionId, bool isTyping)
    {
        var currentUserId = GetUserId();
        if (currentUserId == null)
        {
            return;
        }

        if (!Guid.TryParse(connectionId, out var connectionGuid))
        {
            return;
        }

        var connection = await _connectionService.GetByIdAsync(connectionGuid);
        if (connection == null)
        {
            return;
        }

        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            return;
        }

        var groupName = GetConversationGroupName(connectionId);
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("TypingIndicator", isTyping);
    }

    private string? GetUserId()
    {
        return _identityService.UserId ?? Context.User?.GetUserId();
    }

    private static string GetConversationGroupName(string connectionId)
    {
        return $"conversation_{connectionId}";
    }
}
