namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;

public class UserProfileService : IUserProfileService
{
    private readonly AppDbContext _context;

    public UserProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByUserId(string userId)
    {
        return await _context.UserProfiles
            .Include(up => up.NeurodiverseTraits)
            .Include(up => up.Preference)
            .FirstOrDefaultAsync(up => up.UserId == userId);
    }

    public async Task<UserProfile> CreateOrUpdate(
        string userId,
        DateOnly? doB = null,
        string? preferredPronoun = null,
        IEnumerable<string>? neurodiverseTraits = null,
        string? preferences = null)
    {
        var existingProfile = await GetByUserId(userId);

        if (existingProfile == null)
        {
            // Create new profile
            var newProfile = new UserProfile
            {
                UserId = userId,
                DoB = doB,
                PreferredPronoun = preferredPronoun
            };

            _context.UserProfiles.Add(newProfile);
            await _context.SaveChangesAsync();

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
                    Preferences = preferences
                };
            }

            await _context.SaveChangesAsync();
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
                        Preferences = preferences
                    };
                }
                else
                {
                    existingProfile.Preference.Preferences = preferences;
                }
            }
            else if (existingProfile.Preference != null)
            {
                // Remove preferences if null is passed
                _context.UserPreferences.Remove(existingProfile.Preference);
            }

            _context.UserProfiles.Update(existingProfile);
            await _context.SaveChangesAsync();
            return existingProfile;
        }
    }

    public async Task<bool> DeleteByUserId(string userId)
    {
        var profile = await GetByUserId(userId);
        if (profile == null)
        {
            return false;
        }

        _context.UserProfiles.Remove(profile);
        await _context.SaveChangesAsync();
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
        string? preferences = null);
    Task<bool> DeleteByUserId(string userId);
}

