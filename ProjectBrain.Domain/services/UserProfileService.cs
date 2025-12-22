namespace ProjectBrain.Domain;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _repository;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private const string ProfileCacheKeyPrefix = "userprofile:";
    private static readonly TimeSpan ProfileCacheExpiration = TimeSpan.FromMinutes(30);

    public UserProfileService(IUserProfileRepository repository, AppDbContext context, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UserProfile?> GetByUserId(string userId)
    {
        // Try cache first
        var cacheKey = $"{ProfileCacheKeyPrefix}{userId}";
        var cachedProfile = await _cache.GetAsync<UserProfile>(cacheKey);
        if (cachedProfile != null)
        {
            return cachedProfile;
        }

        var profile = await _repository.GetByUserIdWithRelatedAsync(userId);

        // Cache the profile if found
        if (profile != null)
        {
            await _cache.SetAsync(cacheKey, profile, ProfileCacheExpiration);
        }

        return profile;
    }

    public async Task<UserProfile> CreateOrUpdate(
        string userId,
        DateOnly? doB = null,
        string? preferredPronoun = null,
        IEnumerable<string>? neurodiverseTraits = null,
        Dictionary<string, object>? preferences = null)
    {
        // Get tracked entity for potential update (not using AsNoTracking)
        var existingProfile = await _context.UserProfiles
            .Include(up => up.NeurodiverseTraits)
            .Include(up => up.Preference)
            .FirstOrDefaultAsync(up => up.UserId == userId);

        if (existingProfile == null)
        {
            // Create new profile
            var newProfile = new UserProfile
            {
                UserId = userId,
                DoB = doB,
                PreferredPronoun = preferredPronoun
            };

            _repository.Add(newProfile);
            await _unitOfWork.SaveChangesAsync();

            // Add neurodiverse traits
            if (neurodiverseTraits != null)
            {
                newProfile.NeurodiverseTraits = neurodiverseTraits
                    .Select(t => new NeurodiverseTrait
                    {
                        UserProfileId = newProfile.Id,
                        Trait = t
                    })
                    .ToList();
            }

            // Add preferences
            if (preferences != null)
            {
                newProfile.Preference = new UserPreference
                {
                    UserProfileId = newProfile.Id,
                    Preferences = JsonSerializer.Serialize(preferences)
                };
            }

            await _unitOfWork.SaveChangesAsync();
            return newProfile;
        }
        else
        {
            // Update existing profile
            // Update DoB if provided
            if (doB.HasValue)
            {
                existingProfile.DoB = doB;
            }

            // Update preferred pronoun if provided
            if (preferredPronoun != null)
            {
                existingProfile.PreferredPronoun = preferredPronoun;
            }

            // Remove existing neurodiverse traits
            _context.NeurodiverseTraits.RemoveRange(existingProfile.NeurodiverseTraits);

            // Add new neurodiverse traits
            if (neurodiverseTraits != null)
            {
                existingProfile.NeurodiverseTraits = neurodiverseTraits
                    .Select(t => new NeurodiverseTrait
                    {
                        UserProfileId = existingProfile.Id,
                        Trait = t
                    })
                    .ToList();
            }
            else
            {
                existingProfile.NeurodiverseTraits = new List<NeurodiverseTrait>();
            }

            // Update or create preferences
            if (preferences != null)
            {
                if (existingProfile.Preference == null)
                {
                    existingProfile.Preference = new UserPreference
                    {
                        UserProfileId = existingProfile.Id,
                        Preferences = JsonSerializer.Serialize(preferences)
                    };
                }
                else
                {
                    existingProfile.Preference.Preferences = JsonSerializer.Serialize(preferences);
                }
            }
            else if (existingProfile.Preference != null)
            {
                // Remove preferences if null is passed
                _context.UserPreferences.Remove(existingProfile.Preference);
            }

            _repository.Update(existingProfile);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate cache
            var cacheKey = $"{ProfileCacheKeyPrefix}{userId}";
            await _cache.RemoveAsync(cacheKey);

            return existingProfile;
        }
    }

    public async Task<bool> DeleteByUserId(string userId)
    {
        // Get tracked entity for deletion (not using AsNoTracking)
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(up => up.UserId == userId);
        if (profile == null)
        {
            return false;
        }

        _repository.Remove(profile);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate cache
        var cacheKey = $"{ProfileCacheKeyPrefix}{userId}";
        await _cache.RemoveAsync(cacheKey);

        return true;
    }
}

public interface IUserProfileService
{
    Task<UserProfile?> GetByUserId(string userId);
    Task<UserProfile> CreateOrUpdate(
        string userId,
        DateOnly? doB = null,
        string? preferredPronoun = null,
        IEnumerable<string>? neurodiverseTraits = null,
        Dictionary<string, object>? preferences = null);
    Task<bool> DeleteByUserId(string userId);
}

