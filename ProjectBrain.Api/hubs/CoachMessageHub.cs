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

        var connectionGuid = Guid.Parse(connectionId);
        var connection = await _connectionService.GetByIdAsync(connectionGuid);

        // Verify user has access to this conversation
        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to join conversation {UserId}-{CoachId} without access",
                currentUserId, connection.UserId, connection.CoachId);
            return;
        }

        var groupName = GetConversationGroupName(connectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} joined conversation group {GroupName}", currentUserId, groupName);
    }

    public async Task LeaveConversation(string connectionId)
    {
        _logger.LogInformation("User {UserId} leaving conversation {ConnectionId}", GetUserId(), connectionId);
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

        var connectionGuid = Guid.Parse(connectionId);
        var connection = await _connectionService.GetByIdAsync(connectionGuid);

        // Verify user has access to this conversation
        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            return;
        }

        var groupName = GetConversationGroupName(connectionId);
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("TypingIndicator", currentUserId, isTyping);
    }

    private string? GetUserId()
    {
        // Extract user ID from JWT token in the context
        var userIdClaim = Context.User?.FindFirst("sub")?.Value ??
                         Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return userIdClaim;
    }

    private static string GetConversationGroupName(string connectionId)
    {
        // Create a consistent group name regardless of parameter order
        // var ids = new[] { userId, coachId }.OrderBy(id => id).ToArray();
        // return $"conversation_{ids[0]}_{ids[1]}";
        return $"conversation_{connectionId}";
    }
}

