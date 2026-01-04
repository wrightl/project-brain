namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;

/// <summary>
/// Repository implementation for AgentAction entity
/// </summary>
public class AgentActionRepository : Repository<AgentAction, Guid>, IAgentActionRepository
{
    public AgentActionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AgentAction>> GetByUserIdAsync(string userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.ExecutedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AgentAction>> GetByToolNameAsync(string toolName, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.ToolName == toolName)
            .OrderByDescending(a => a.ExecutedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AgentAction>> GetRecentActionsAsync(string userId, int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.ExecutedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByToolAsync(string toolName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(a => a.ToolName == toolName, cancellationToken);
    }

    public async Task<IEnumerable<AgentAction>> GetByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.WorkflowId == workflowId)
            .OrderBy(a => a.ExecutedAt)
            .ToListAsync(cancellationToken);
    }
}

