namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class ConnectionWithStatus
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string CoachId { get; init; }
    public required string Status { get; init; }
    public string? UserName { get; init; }
    public string? CoachName { get; init; }
    public string? CoachProfileId { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? RespondedAt { get; init; }

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
    private readonly IConnectionRepository _repository;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ConnectionService(IConnectionRepository repository, AppDbContext context, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<Connection?> GetConnectionAsync(string userId, string coachId)
    {
        return await _repository.GetByUserAndCoachAsync(userId, coachId);
    }

    public async Task<Connection> CreateConnectionRequestAsync(
        string userId,
        string coachId,
        string requestedBy,
        string? message = null)
    {
        // Check if connection already exists (get tracked entity for potential update)
        var existingConnection = await _context.Connections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CoachId == coachId);

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
                await _unitOfWork.SaveChangesAsync();
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
        await _unitOfWork.SaveChangesAsync();
        return connection;
    }

    public async Task<bool> AcceptConnectionAsync(string userId, string coachId)
    {
        // Get tracked entity for update (not using AsNoTracking)
        var connection = await _context.Connections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CoachId == coachId);

        if (connection == null || connection.Status != "pending")
        {
            return false;
        }

        connection.Status = "accepted";
        connection.RespondedAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;

        _context.Connections.Update(connection);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelOrDeleteConnectionAsync(Guid connectionId)
    {
        // Get tracked entity for update/delete (not using AsNoTracking)
        var connection = await _context.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId);

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

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectConnectionAsync(string userId, string coachId)
    {
        // Get tracked entity for update (not using AsNoTracking)
        var connection = await _context.Connections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CoachId == coachId);

        if (connection == null || connection.Status != "pending")
        {
            return false;
        }

        connection.Status = "rejected";
        connection.RespondedAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;

        _context.Connections.Update(connection);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Gets all coach IDs that the user has an active connection with (accepted or pending).
    /// Returns the coach ID and connection status.
    /// </summary>
    public async Task<List<ConnectionWithStatus>> GetConnectedCoachIdsAsync(string userId)
    {
        var connections = await _repository.GetConnectedCoachesAsync(userId);
        return connections.Select(c => ConnectionWithStatus.FromConnection(c)).ToList();
    }

    /// <summary>
    /// Gets all user IDs that the coach has an active connection with (accepted or pending).
    /// Returns the user ID and connection status.
    /// </summary>
    public async Task<List<ConnectionWithStatus>> GetConnectionsAsync(string userId, bool isCoach)
    {
        var connections = await _repository.GetConnectionsAsync(userId, isCoach);
        return connections.Select(c => new ConnectionWithStatus
        {
            Id = c.Id.ToString(),
            Status = c.Status,
            UserId = c.UserId,
            CoachId = c.CoachId,
            UserName = isCoach ? (c.User != null ? c.User.FullName : null) : (c.Coach != null ? c.Coach.FullName : null),
            CoachName = isCoach ? (c.Coach != null ? c.Coach.FullName : null) : (c.User != null ? c.User.FullName : null),
            RequestedAt = c.RequestedAt,
            RespondedAt = c.RespondedAt
        }).ToList();
    }

    /// <summary>
    /// Gets all user IDs that the coach has an active connection with (accepted or pending).
    /// Returns the user ID and connection status.
    /// </summary>
    public async Task<List<ConnectionWithStatus>> GetConnectedUserIdsAsync(string coachId)
    {
        var connections = await _repository.GetConnectedUsersAsync(coachId);
        return connections.Select(c => ConnectionWithStatus.FromConnection(c)).ToList();
    }

    /// <summary>
    /// Gets all connections for a coach (both accepted and pending).
    /// Returns the user ID and connection status for each connection.
    /// </summary>
    public async Task<List<ConnectionWithStatus>> GetConnectionsByCoachIdAsync(string coachId)
    {
        var connections = await _repository.GetConnectionsByCoachIdAsync(coachId);
        return connections.Select(c => ConnectionWithStatus.FromConnection(c)).ToList();
    }

    /// <summary>
    /// Gets the earliest connection date for a user (approximate account creation date).
    /// Returns null if the user has no connections.
    /// </summary>
    public async Task<DateTime?> GetEarliestConnectionDateAsync(string userId)
    {
        return await _repository.GetEarliestConnectionDateAsync(userId);
    }

    /// <summary>
    /// Gets a connection by its ID.
    /// Returns the connection or null if it does not exist.
    /// </summary>
    public async Task<Connection?> GetByIdAsync(Guid connectionId)
    {
        return await _repository.GetByIdAsync(connectionId);
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

