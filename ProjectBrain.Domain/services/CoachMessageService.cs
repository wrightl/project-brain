using Microsoft.EntityFrameworkCore;

namespace ProjectBrain.Domain;

public class CoachMessageService : ICoachMessageService
{
    private readonly AppDbContext _context;

    public CoachMessageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CoachMessage> Add(CoachMessage coachMessage)
    {
        _context.CoachMessages.Add(coachMessage);
        await _context.SaveChangesAsync();
        return coachMessage;
    }

    public async Task<CoachMessage?> GetById(Guid id)
    {
        return await _context.CoachMessages
            .Include(cm => cm.Sender)
            .Include(cm => cm.User)
            .Include(cm => cm.Coach)
            .FirstOrDefaultAsync(cm => cm.Id == id);
    }

    public async Task<IEnumerable<CoachMessage>> GetByConnectionId(Guid connectionId)
    {
        return await _context.CoachMessages
            .Include(cm => cm.Sender)
            .Include(cm => cm.User)
            .Include(cm => cm.Coach)
            .Where(cm => cm.ConnectionId == connectionId)
            .OrderByDescending(cm => cm.CreatedAt)
            .ToListAsync();
    }

    // public async Task<IEnumerable<CoachMessage>> GetAll()
    // {
    //     return await _context.CoachMessages
    //         .Include(cm => cm.Sender)
    //         .Include(cm => cm.User)
    //         .Include(cm => cm.Coach)
    //         .ToListAsync();
    // }

    public async Task<IEnumerable<CoachMessage>> GetByCoachId(string coachId)
    {
        return await _context.CoachMessages
            .Include(cm => cm.Sender)
            .Include(cm => cm.User)
            .Include(cm => cm.Coach)
            .Where(cm => cm.CoachId == coachId)
            .OrderByDescending(cm => cm.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<CoachMessage>> GetByUserId(string userId)
    {
        return await _context.CoachMessages
            .Include(cm => cm.Sender)
            .Include(cm => cm.User)
            .Include(cm => cm.Coach)
            .Where(cm => cm.UserId == userId)
            .OrderByDescending(cm => cm.CreatedAt)
            .ToListAsync();
    }

    public async Task<CoachMessage> Update(CoachMessage coachMessage)
    {
        _context.CoachMessages.Update(coachMessage);
        await _context.SaveChangesAsync();
        return coachMessage;
    }

    public async Task<bool> Delete(Guid id)
    {
        var coachMessage = await GetById(id);
        if (coachMessage == null)
        {
            return false;
        }
        _context.CoachMessages.Remove(coachMessage);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CoachMessage>> GetConversationMessagesAsync(
        Guid connectionId,
        int pageSize = 20,
        DateTime? beforeDate = null)
    {
        var query = _context.CoachMessages
            .Include(cm => cm.Sender)
            .Include(cm => cm.User)
            .Include(cm => cm.Coach)
            .Where(cm =>
                cm.ConnectionId == connectionId);

        if (beforeDate.HasValue)
        {
            query = query.Where(cm => cm.CreatedAt < beforeDate.Value);
        }

        return await query
            .OrderByDescending(cm => cm.CreatedAt)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<CoachMessage>> SearchMessagesAsync(
        Guid connectionId,
        string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await _context.CoachMessages
            .Include(cm => cm.Sender)
            .Include(cm => cm.User)
            .Include(cm => cm.Coach)
            .Where(cm =>
                cm.ConnectionId == connectionId &&
                (cm.MessageType == "text" && cm.Content.ToLower().Contains(lowerSearchTerm)))
            .OrderByDescending(cm => cm.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkAsDeliveredAsync(Guid messageId, string recipientId)
    {
        var message = await _context.CoachMessages.FindAsync(messageId);
        if (message == null || message.SenderId == recipientId)
        {
            return false;
        }

        if (message.Status == "sent")
        {
            message.Status = "delivered";
            message.DeliveredAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> MarkAsReadAsync(Guid messageId, string recipientId)
    {
        var message = await _context.CoachMessages.FindAsync(messageId);
        if (message == null || message.SenderId == recipientId)
        {
            return false;
        }

        if (message.Status != "read")
        {
            message.Status = "read";
            message.ReadAt = DateTime.UtcNow;
            if (!message.DeliveredAt.HasValue)
            {
                message.DeliveredAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task MarkConversationAsReadAsync(Guid connectionId, string currentUserId)
    {
        var messages = await _context.CoachMessages
            .Where(cm =>
                cm.ConnectionId == connectionId &&
                cm.SenderId != currentUserId &&
                cm.Status != "read")
            .ToListAsync();

        foreach (var message in messages)
        {
            message.Status = "read";
            message.ReadAt = DateTime.UtcNow;
            if (!message.DeliveredAt.HasValue)
            {
                message.DeliveredAt = DateTime.UtcNow;
            }
        }

        if (messages.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets all conversations for a user (either as user or coach) with last message and unread count.
    /// Returns conversations where the user is either the UserId or CoachId in accepted connections.
    /// </summary>
    public async Task<List<ConversationSummary>> GetConversationsAsync(string userId, bool isCoach)
    {
        // Get all accepted connections where user is either UserId or CoachId
        var connections = await _context.Connections
            .Include(c => c.User)
            .Include(c => c.Coach)
            .Where(c =>
                ((c.UserId == userId && !isCoach) || (c.CoachId == userId && isCoach)) &&
                c.Status == "accepted")
            .ToListAsync();

        var conversationSummaries = new List<ConversationSummary>();

        foreach (var connection in connections)
        {
            // Get last message for this connection
            var lastMessage = await _context.CoachMessages
                .Include(cm => cm.Sender)
                .Where(cm => cm.ConnectionId == connection.Id)
                .OrderByDescending(cm => cm.CreatedAt)
                .FirstOrDefaultAsync();

            // Get unread count (messages where SenderId != userId and ReadAt is null)
            var unreadCount = await _context.CoachMessages
                .CountAsync(cm =>
                    cm.ConnectionId == connection.Id &&
                    cm.SenderId != userId &&
                    cm.ReadAt == null);

            // Determine other person's info
            string otherPersonName;
            string otherPersonId;
            if (connection.UserId == userId)
            {
                // User viewing coach
                otherPersonName = connection.Coach?.FullName ?? "Unknown";
                otherPersonId = connection.CoachId;
            }
            else
            {
                // Coach viewing user
                otherPersonName = connection.User?.FullName ?? "Unknown";
                otherPersonId = connection.UserId;
            }

            // Get last message snippet
            string? lastMessageSnippet = null;
            string? lastMessageSenderName = null;
            DateTime? lastMessageTimestamp = null;

            if (lastMessage != null)
            {
                lastMessageTimestamp = lastMessage.CreatedAt;
                lastMessageSenderName = lastMessage.Sender?.FullName ?? "Unknown";

                if (lastMessage.MessageType == "text")
                {
                    lastMessageSnippet = lastMessage.Content.Length > 50
                        ? lastMessage.Content.Substring(0, 50) + "..."
                        : lastMessage.Content;
                }
                else
                {
                    lastMessageSnippet = "Voice message";
                }
            }

            conversationSummaries.Add(new ConversationSummary
            {
                ConnectionId = connection.Id,
                OtherPersonName = otherPersonName,
                OtherPersonId = otherPersonId,
                LastMessageSnippet = lastMessageSnippet,
                LastMessageSenderName = lastMessageSenderName,
                LastMessageTimestamp = lastMessageTimestamp,
                UnreadCount = unreadCount
            });
        }

        return conversationSummaries;
    }
}

public class ConversationSummary
{
    public Guid ConnectionId { get; set; }
    public string OtherPersonName { get; set; } = string.Empty;
    public string OtherPersonId { get; set; } = string.Empty;
    public string? LastMessageSnippet { get; set; }
    public string? LastMessageSenderName { get; set; }
    public DateTime? LastMessageTimestamp { get; set; }
    public int UnreadCount { get; set; }
}

public interface ICoachMessageService
{
    Task<CoachMessage> Add(CoachMessage coachMessage);
    Task<CoachMessage?> GetById(Guid id);
    Task<IEnumerable<CoachMessage>> GetByConnectionId(Guid connectionId);
    Task<IEnumerable<CoachMessage>> GetByCoachId(string coachId);
    Task<IEnumerable<CoachMessage>> GetByUserId(string userId);
    // Task<IEnumerable<CoachMessage>> GetAll();
    Task<CoachMessage> Update(CoachMessage coachMessage);
    Task<bool> Delete(Guid id);
    Task<IEnumerable<CoachMessage>> GetConversationMessagesAsync(Guid connectionId, int pageSize = 20, DateTime? beforeDate = null);
    Task<IEnumerable<CoachMessage>> SearchMessagesAsync(Guid connectionId, string searchTerm);
    Task<bool> MarkAsDeliveredAsync(Guid messageId, string recipientId);
    Task<bool> MarkAsReadAsync(Guid messageId, string recipientId);
    Task MarkConversationAsReadAsync(Guid connectionId, string currentUserId);
    Task<List<ConversationSummary>> GetConversationsAsync(string userId, bool isCoach);
}