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
    private readonly ICoachSpecialismOptionService _coachSpecialismOptionService;

    public CoachProfileService(
        ICoachProfileRepository repository,
        AppDbContext context,
        IUnitOfWork unitOfWork,
        ICoachSpecialismOptionService coachSpecialismOptionService)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
        _coachSpecialismOptionService = coachSpecialismOptionService;
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
        IEnumerable<string>? ageGroups = null,
        string? bio = null,
        string? imageUrl = null)
    {
        await _coachSpecialismOptionService.ValidateSpecialismsAsync(specialisms);

        var existingProfile = await GetByUserId(userId);

        if (existingProfile == null)
        {
            // Create new profile
            var newProfile = new CoachProfile
            {
                UserId = userId,
                Bio = bio,
                ImageUrl = imageUrl
            };

            _repository.Add(newProfile);
            await _unitOfWork.SaveChangesAsync();

            // Get tracked entity to add related entities
            var trackedProfile = await _context.CoachProfiles
                .FirstOrDefaultAsync(cp => cp.Id == newProfile.Id);

            if (trackedProfile != null)
            {
                // Update optional profile fields
                if (bio != null)
                {
                    trackedProfile.Bio = bio;
                }
                if (imageUrl != null)
                {
                    trackedProfile.ImageUrl = imageUrl;
                }

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
            // Get tracked entity for update
            var trackedProfile = await _context.CoachProfiles
                .Include(cp => cp.Qualifications)
                .Include(cp => cp.Specialisms)
                .Include(cp => cp.AgeGroups)
                .FirstOrDefaultAsync(cp => cp.Id == existingProfile.Id);

            if (trackedProfile != null)
            {
                if (bio != null)
                {
                    trackedProfile.Bio = bio;
                }
                if (imageUrl != null)
                {
                    trackedProfile.ImageUrl = imageUrl;
                }

                // Null collections mean leave existing rows unchanged. The coach
                // profile PUT omits empty arrays, so treating null as "clear"
                // deleted qualifications/specialisms/age groups on partial edits.
                if (qualifications != null)
                {
                    _context.CoachQualifications.RemoveRange(trackedProfile.Qualifications);
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
                    _context.CoachSpecialisms.RemoveRange(trackedProfile.Specialisms);
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
                    _context.CoachAgeGroups.RemoveRange(trackedProfile.AgeGroups);
                    trackedProfile.AgeGroups = ageGroups
                        .Select(ag => new CoachAgeGroup
                        {
                            CoachProfileId = trackedProfile.Id,
                            AgeGroup = ag
                        })
                        .ToList();
                }

                _repository.Update(trackedProfile);
            }

            await _unitOfWork.SaveChangesAsync();
            return trackedProfile ?? existingProfile;
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

    public async Task<List<CoachProfile>> SearchByDistance(
        double centerLatitude,
        double centerLongitude,
        double radiusMiles,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null)
    {
        var results = await _repository.SearchByDistanceAsync(
            centerLatitude,
            centerLongitude,
            radiusMiles,
            ageGroups,
            specialisms);
        return results.ToList();
    }

    public async Task<List<CoachProfile>> GetByIdsWithUserAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var results = await _repository.GetByIdsWithUserAsync(ids, cancellationToken);
        return results.ToList();
    }

    public async Task<List<CoachProfile>> GetByUserIdsWithRelatedAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var results = await _repository.GetByUserIdsWithRelatedAsync(userIds, cancellationToken);
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
        IEnumerable<string>? ageGroups = null,
        string? bio = null,
        string? imageUrl = null);

    Task<bool> UpdateAvailabilityStatus(string userId, AvailabilityStatus status);

    Task<bool> DeleteByUserId(string userId);
    Task<List<CoachProfile>> Search(
        string? city = null,
        string? stateProvince = null,
        string? country = null,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null);

    Task<List<CoachProfile>> SearchByDistance(
        double centerLatitude,
        double centerLongitude,
        double radiusMiles,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null);

    Task<List<CoachProfile>> GetByIdsWithUserAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default);

    Task<List<CoachProfile>> GetByUserIdsWithRelatedAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default);
}

