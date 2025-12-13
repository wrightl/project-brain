namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;

public class ConnectionWithStatus
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string CoachId { get; init; }
    public required string Status { get; init; }
    public string? UserName { get; init; }
    public string? CoachName { get; init; }
    public DateTime RequestedAt { get; internal set; }
    public DateTime? RespondedAt { get; internal set; }

    public static ConnectionWithStatus FromConnection(Connection connection)
    {
        return new ConnectionWithStatus
        {
            Id = connection.Id.ToString(),
            UserId = connection.UserId,
            CoachId = connection.CoachId,
            Status = connection.Status,
            UserName = connection.User?.FullName,
            CoachName = connection.Coach?.FullName,
            RequestedAt = connection.RequestedAt,
            RespondedAt = connection.RespondedAt
        };
    }
}

public class ConnectionService : IConnectionService
{
    private readonly AppDbContext _context;

    public ConnectionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Connection?> GetConnectionAsync(string userId, string coachId)
    {
        return await _context.Connections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CoachId == coachId);
    }

    public async Task<Connection> CreateConnectionRequestAsync(
        string userId,
        string coachId,
        string requestedBy,
        string? message = null)
    {
        // Check if connection already exists
        var existingConnection = await GetConnectionAsync(userId, coachId);

        if (existingConnection != null)
        {
            // If connection exists and is cancelled or rejected, allow creating a new one
            if (existingConnection.Status == "cancelled" || existingConnection.Status == "rejected")
            {
                // Update the existing connection to pending
                existingConnection.Status = "pending";
                existingConnection.RequestedBy = requestedBy;
                existingConnection.Message = message;
                existingConnection.RequestedAt = DateTime.UtcNow;
                existingConnection.RespondedAt = null;
                existingConnection.UpdatedAt = DateTime.UtcNow;

                _context.Connections.Update(existingConnection);
                await _context.SaveChangesAsync();
                return existingConnection;
            }

            // If connection already exists in pending or accepted state, return it (idempotent)
            return existingConnection;
        }

        // Create new connection request
        var connection = new Connection
        {
            UserId = userId,
            CoachId = coachId,
            Status = "pending",
            RequestedBy = requestedBy,
            Message = message,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Connections.Add(connection);
        await _context.SaveChangesAsync();
        return connection;
    }

    public async Task<bool> AcceptConnectionAsync(string userId, string coachId)
    {
        var connection = await GetConnectionAsync(userId, coachId);

        if (connection == null || connection.Status != "pending")
        {
            return false;
        }

        connection.Status = "accepted";
        connection.RespondedAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;

        _context.Connections.Update(connection);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelOrDeleteConnectionAsync(Guid connectionId)
    {
        var connection = await GetByIdAsync(connectionId);

        if (connection == null)
        {
            // Return true for idempotency - connection doesn't exist, so it's already "deleted"
            return true;
        }

        // If pending, mark as cancelled; if accepted, delete it
        if (connection.Status == "pending")
        {
            connection.Status = "cancelled";
            connection.UpdatedAt = DateTime.UtcNow;
            _context.Connections.Update(connection);
        }
        else
        {
            // Delete accepted connections
            _context.Connections.Remove(connection);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectConnectionAsync(string userId, string coachId)
    {
        var connection = await GetConnectionAsync(userId, coachId);

        if (connection == null || connection.Status != "pending")
        {
            return false;
        }

        connection.Status = "rejected";
        connection.RespondedAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;

        _context.Connections.Update(connection);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Gets all coach IDs that the user has an active connection with (accepted or pending).
    /// Returns the coach ID and connection status.
    /// </summary>
    public async Task<List<ConnectionWithStatus>> GetConnectedCoachIdsAsync(string userId)
    {
        return await _context.Connections
            .Where(c => c.UserId == userId && (c.Status == "accepted" || c.Status == "pending"))
            .Select(c => ConnectionWithStatus.FromConnection(c))
            .ToListAsync();
    }

    /// <summary>
    /// Gets all user IDs that the coach has an active connection with (accepted or pending).
    /// Returns the user ID and connection status.
    /// </summary>
    public async Task<List<ConnectionWithStatus>> GetConnectionsAsync(string userId, bool isCoach)
    {
        return await _context.Connections
            .Where(c => (c.UserId == userId && !isCoach) || (c.CoachId == userId && isCoach) && (c.Status == "accepted" || c.Status == "pending"))
            .Include(c => c.User)
            .Include(c => c.Coach)
            .Select(c => new ConnectionWithStatus
            {
                Id = c.Id.ToString(),
                Status = c.Status,
                UserId = c.UserId,
                CoachId = c.CoachId,
                UserName = isCoach ? c.User.FullName : c.Coach.FullName,
                CoachName = isCoach ? c.Coach.FullName : c.User.FullName,
                RequestedAt = c.RequestedAt,
                RespondedAt = c.RespondedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Gets all user IDs that the coach has an active connection with (accepted or pending).
    /// Returns the user ID and connection status.
    /// </summary>
    public async Task<List<ConnectionWithStatus>> GetConnectedUserIdsAsync(string coachId)
    {
        return await _context.Connections
            .Where(c => c.CoachId == coachId && (c.Status == "accepted" || c.Status == "pending"))
            .Select(c => ConnectionWithStatus.FromConnection(c))
            .ToListAsync();
    }

    /// <summary>
    /// Gets all connections for a coach (both accepted and pending).
    /// Returns the user ID and connection status for each connection.
    /// </summary>
    public async Task<List<ConnectionWithStatus>> GetConnectionsByCoachIdAsync(string coachId)
    {
        return await _context.Connections
            .Where(c => c.CoachId == coachId && (c.Status == "accepted" || c.Status == "pending"))
            .Select(c => ConnectionWithStatus.FromConnection(c))
            .ToListAsync();
    }

    /// <summary>
    /// Gets the earliest connection date for a user (approximate account creation date).
    /// Returns null if the user has no connections.
    /// </summary>
    public async Task<DateTime?> GetEarliestConnectionDateAsync(string userId)
    {
        var earliestConnection = await _context.Connections
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        return earliestConnection?.CreatedAt;
    }

    /// <summary>
    /// Gets a connection by its ID.
    /// Returns the connection or null if it does not exist.
    /// </summary>
    public async Task<Connection?> GetByIdAsync(Guid connectionId)
    {
        return await _context.Connections.FirstOrDefaultAsync(c => c.Id == connectionId);
    }
}

public interface IConnectionService
{
    Task<Connection?> GetConnectionAsync(string userId, string coachId);
    Task<Connection> CreateConnectionRequestAsync(
        string userId,
        string coachId,
        string requestedBy,
        string? message = null);
    Task<bool> AcceptConnectionAsync(string userId, string coachId);
    Task<bool> CancelOrDeleteConnectionAsync(Guid connectionId);
    Task<bool> RejectConnectionAsync(string userId, string coachId);
    Task<List<ConnectionWithStatus>> GetConnectedCoachIdsAsync(string userId);
    Task<List<ConnectionWithStatus>> GetConnectedUserIdsAsync(string coachId);
    Task<List<ConnectionWithStatus>> GetConnectionsByCoachIdAsync(string coachId);
    Task<DateTime?> GetEarliestConnectionDateAsync(string userId);
    Task<Connection?> GetByIdAsync(Guid connectionId);
    Task<List<ConnectionWithStatus>> GetConnectionsAsync(string userId, bool isCoach);
}

