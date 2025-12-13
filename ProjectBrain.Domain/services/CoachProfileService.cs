namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;

public class CoachProfileService : ICoachProfileService
{
    private readonly AppDbContext _context;

    public CoachProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CoachProfile?> GetById(int id)
    {
        return await _context.CoachProfiles
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
            .FirstOrDefaultAsync(cp => cp.Id == id);
    }

    public async Task<CoachProfile?> GetByUserId(string userId)
    {
        return await _context.CoachProfiles
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
                .ThenInclude(u => u!.UserRoles)
            .FirstOrDefaultAsync(cp => cp.UserId == userId);
    }

    public async Task<CoachProfile> CreateOrUpdate(
        string userId,
        IEnumerable<string>? qualifications = null,
        IEnumerable<string>? specialisms = null,
        IEnumerable<string>? ageGroups = null)
    {
        var existingProfile = await GetByUserId(userId);

        if (existingProfile == null)
        {
            // Create new profile
            var newProfile = new CoachProfile
            {
                UserId = userId
            };

            _context.CoachProfiles.Add(newProfile);
            await _context.SaveChangesAsync();

            // Add related entities
            if (qualifications != null)
            {
                newProfile.Qualifications = qualifications
                    .Select(q => new CoachQualification
                    {
                        CoachProfileId = newProfile.Id,
                        Qualification = q
                    })
                    .ToList();
            }

            if (specialisms != null)
            {
                newProfile.Specialisms = specialisms
                    .Select(s => new CoachSpecialism
                    {
                        CoachProfileId = newProfile.Id,
                        Specialism = s
                    })
                    .ToList();
            }

            if (ageGroups != null)
            {
                newProfile.AgeGroups = ageGroups
                    .Select(ag => new CoachAgeGroup
                    {
                        CoachProfileId = newProfile.Id,
                        AgeGroup = ag
                    })
                    .ToList();
            }

            await _context.SaveChangesAsync();
            return newProfile;
        }
        else
        {
            // Update existing profile
            // Remove existing related entities
            _context.CoachQualifications.RemoveRange(existingProfile.Qualifications);
            _context.CoachSpecialisms.RemoveRange(existingProfile.Specialisms);
            _context.CoachAgeGroups.RemoveRange(existingProfile.AgeGroups);

            // Add new related entities
            if (qualifications != null)
            {
                existingProfile.Qualifications = qualifications
                    .Select(q => new CoachQualification
                    {
                        CoachProfileId = existingProfile.Id,
                        Qualification = q
                    })
                    .ToList();
            }
            else
            {
                existingProfile.Qualifications = new List<CoachQualification>();
            }

            if (specialisms != null)
            {
                existingProfile.Specialisms = specialisms
                    .Select(s => new CoachSpecialism
                    {
                        CoachProfileId = existingProfile.Id,
                        Specialism = s
                    })
                    .ToList();
            }
            else
            {
                existingProfile.Specialisms = new List<CoachSpecialism>();
            }

            if (ageGroups != null)
            {
                existingProfile.AgeGroups = ageGroups
                    .Select(ag => new CoachAgeGroup
                    {
                        CoachProfileId = existingProfile.Id,
                        AgeGroup = ag
                    })
                    .ToList();
            }
            else
            {
                existingProfile.AgeGroups = new List<CoachAgeGroup>();
            }

            _context.CoachProfiles.Update(existingProfile);
            await _context.SaveChangesAsync();
            return existingProfile;
        }
    }

    public async Task<bool> UpdateAvailabilityStatus(string userId, AvailabilityStatus status)
    {
        var coachProfile = await GetByUserId(userId);
        if (coachProfile == null)
        {
            return false;
        }

        coachProfile.AvailabilityStatus = status;
        _context.CoachProfiles.Update(coachProfile);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByUserId(string userId)
    {
        var profile = await GetByUserId(userId);
        if (profile == null)
        {
            return false;
        }

        _context.CoachProfiles.Remove(profile);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<CoachProfile>> Search(
        string? city = null,
        string? stateProvince = null,
        string? country = null,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null)
    {
        var query = _context.CoachProfiles
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
                .ThenInclude(u => u!.UserRoles)
            .AsQueryable();

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

        return await query.ToListAsync();
    }
}

public interface ICoachProfileService
{
    Task<CoachProfile?> GetById(int id);
    Task<CoachProfile?> GetByUserId(string userId);
    Task<CoachProfile> CreateOrUpdate(
        string userId,
        IEnumerable<string>? qualifications = null,
        IEnumerable<string>? specialisms = null,
        IEnumerable<string>? ageGroups = null);

    Task<bool> UpdateAvailabilityStatus(string userId, AvailabilityStatus status);

    Task<bool> DeleteByUserId(string userId);
    Task<List<CoachProfile>> Search(
        string? city = null,
        string? stateProvince = null,
        string? country = null,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null);
}

