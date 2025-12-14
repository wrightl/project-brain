namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;

/// <summary>
/// Repository implementation for CoachProfile entity
/// </summary>
public class CoachProfileRepository : Repository<CoachProfile, int>, ICoachProfileRepository
{
    public CoachProfileRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CoachProfile?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.Id == id, cancellationToken);
    }

    public async Task<CoachProfile?> GetByIdWithRelatedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
            .FirstOrDefaultAsync(cp => cp.Id == id, cancellationToken);
    }

    public async Task<CoachProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.UserId == userId, cancellationToken);
    }

    public async Task<CoachProfile?> GetByUserIdWithRelatedAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
                .ThenInclude(u => u!.UserRoles)
            .FirstOrDefaultAsync(cp => cp.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<CoachProfile>> SearchAsync(
        string? city = null,
        string? stateProvince = null,
        string? country = null,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CoachProfile> query = _dbSet
            .AsNoTracking()
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
                .ThenInclude(u => u!.UserRoles);

        // Filter by location
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(cp => cp.User != null && cp.User.City != null && cp.User.City.Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(stateProvince))
        {
            query = query.Where(cp => cp.User != null && cp.User.StateProvince != null && cp.User.StateProvince.Contains(stateProvince));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(cp => cp.User != null && cp.User.Country != null && cp.User.Country.Contains(country));
        }

        // Filter by age groups
        if (ageGroups != null && ageGroups.Any())
        {
            var ageGroupList = ageGroups.ToList();
            query = query.Where(cp => cp.AgeGroups == null || cp.AgeGroups.Count == 0 || cp.AgeGroups.Any(ag => ageGroupList.Contains(ag.AgeGroup)));
        }

        // Filter by specialisms
        if (specialisms != null && specialisms.Any())
        {
            var specialismList = specialisms.ToList();
            query = query.Where(cp => cp.Specialisms == null || cp.Specialisms.Count == 0 || cp.Specialisms.Any(s => specialismList.Contains(s.Specialism)));
        }

        // Only return coaches that are onboarded
        query = query.Where(cp => cp.User != null && cp.User.IsOnboarded);

        return await query.ToListAsync(cancellationToken);
    }
}

