namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class CoachSpecialismOptionRepository : ICoachSpecialismOptionRepository
{
    private readonly AppDbContext _context;

    public CoachSpecialismOptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetActiveNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CoachSpecialismOptions
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.Name)
            .Select(o => o.Name)
            .ToListAsync(cancellationToken);
    }
}
