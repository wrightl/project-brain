namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class CoachProfileService : ICoachProfileService
{
    private readonly ICoachProfileRepository _repository;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public CoachProfileService(ICoachProfileRepository repository, AppDbContext context, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<CoachProfile?> GetById(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<CoachProfile?> GetByIdWithRelated(int id)
    {
        return await _repository.GetByIdWithRelatedAsync(id);
    }

    public async Task<CoachProfile?> GetByUserId(string userId)
    {
        return await _repository.GetByUserIdWithRelatedAsync(userId);
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

            _repository.Add(newProfile);
            await _unitOfWork.SaveChangesAsync();

            // Get tracked entity to add related entities
            var trackedProfile = await _context.CoachProfiles
                .FirstOrDefaultAsync(cp => cp.Id == newProfile.Id);

            if (trackedProfile != null)
            {
                // Add related entities
                if (qualifications != null)
                {
                    trackedProfile.Qualifications = qualifications
                        .Select(q => new CoachQualification
                        {
                            CoachProfileId = trackedProfile.Id,
                            Qualification = q
                        })
                        .ToList();
                }

                if (specialisms != null)
                {
                    trackedProfile.Specialisms = specialisms
                        .Select(s => new CoachSpecialism
                        {
                            CoachProfileId = trackedProfile.Id,
                            Specialism = s
                        })
                        .ToList();
                }

                if (ageGroups != null)
                {
                    trackedProfile.AgeGroups = ageGroups
                        .Select(ag => new CoachAgeGroup
                        {
                            CoachProfileId = trackedProfile.Id,
                            AgeGroup = ag
                        })
                        .ToList();
                }

                await _unitOfWork.SaveChangesAsync();
                return trackedProfile;
            }

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

            // Get tracked entity for update
            var trackedProfile = await _context.CoachProfiles
                .Include(cp => cp.Qualifications)
                .Include(cp => cp.Specialisms)
                .Include(cp => cp.AgeGroups)
                .FirstOrDefaultAsync(cp => cp.Id == existingProfile.Id);

            if (trackedProfile != null)
            {
                // Remove existing related entities
                _context.CoachQualifications.RemoveRange(trackedProfile.Qualifications);
                _context.CoachSpecialisms.RemoveRange(trackedProfile.Specialisms);
                _context.CoachAgeGroups.RemoveRange(trackedProfile.AgeGroups);

                // Set new related entities
                trackedProfile.Qualifications = existingProfile.Qualifications;
                trackedProfile.Specialisms = existingProfile.Specialisms;
                trackedProfile.AgeGroups = existingProfile.AgeGroups;

                _repository.Update(trackedProfile);
            }

            await _unitOfWork.SaveChangesAsync();
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

        // Get tracked entity for update
        var trackedProfile = await _context.CoachProfiles
            .FirstOrDefaultAsync(cp => cp.Id == coachProfile.Id);
        if (trackedProfile != null)
        {
            trackedProfile.AvailabilityStatus = status;
            _repository.Update(trackedProfile);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByUserId(string userId)
    {
        var profile = await GetByUserId(userId);
        if (profile == null)
        {
            return false;
        }

        // Get tracked entity for deletion
        var trackedProfile = await _context.CoachProfiles
            .FirstOrDefaultAsync(cp => cp.Id == profile.Id);

        if (trackedProfile != null)
        {
            _repository.Remove(trackedProfile);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<List<CoachProfile>> Search(
        string? city = null,
        string? stateProvince = null,
        string? country = null,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null)
    {
        var results = await _repository.SearchAsync(city, stateProvince, country, ageGroups, specialisms);
        return results.ToList();
    }
}

public interface ICoachProfileService
{
    Task<CoachProfile?> GetById(int id);
    Task<CoachProfile?> GetByIdWithRelated(int id);
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

