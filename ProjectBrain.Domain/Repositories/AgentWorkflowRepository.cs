namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;

/// <summary>
/// Repository implementation for AgentWorkflow entity
/// </summary>
public class AgentWorkflowRepository : Repository<AgentWorkflow, Guid>, IAgentWorkflowRepository
{
    public AgentWorkflowRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<AgentWorkflow?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<AgentWorkflow>> GetActiveWorkflowsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(w => w.UserId == userId && (w.Status == "active" || w.Status == "paused"))
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AgentWorkflow>> GetPausedWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(w => w.Status == "paused")
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AgentWorkflow>> GetByStatusAsync(string userId, string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Status == status)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}

