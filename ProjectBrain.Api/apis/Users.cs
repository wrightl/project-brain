
using Microsoft.Extensions.Caching.Memory;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;

public class UserServices(
    ILogger<UserServices> logger,
    IIdentityService identityService,
    IUserService userService,
    IRoleManagement roleManagementService,
    IMemoryCache memoryCache,
    FeatureFlagService featureFlagService,
    IConfiguration configuration,
    ICoachProfileService coachProfileService,
    IUserProfileService userProfileService,
    IUserActivityService userActivityService)
{
    public ILogger<UserServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IUserService UserService { get; } = userService;
    public IRoleManagement RoleManagementService { get; } = roleManagementService;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IConfiguration Configuration { get; } = configuration;
    public FeatureFlagService FeatureFlagService { get; } = featureFlagService;
    public ICoachProfileService CoachProfileService { get; } = coachProfileService;
    public IUserProfileService UserProfileService { get; } = userProfileService;
    public IUserActivityService UserActivityService { get; } = userActivityService;
}

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("users").RequireAuthorization();

        // User endpoints
        group.MapPost("/me/onboarding", OnboardUser).WithName("OnboardUser");
        group.MapPost("/me/onboarding/coach", OnboardCoach).WithName("OnboardCoach");
        group.MapGet("/me", GetCurrentUser).WithName("GetCurrentUser");
        group.MapPut("/me/{userId}", UpdateUser).WithName("UpdateUser");
        group.MapGet("/roles", GetCurrentUserRoles).WithName("GetCurrentUserRoles");

        if (app.Environment.IsDevelopment())
        {
            group.MapGet("/{email}", GetUserByEmail).WithName("GetUserByEmail");
        }
    }

    private static async Task<IResult> OnboardUser([AsParameters] UserServices services, CreateUserRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }
        // if (!string.Equals(request.Role, "user", StringComparison.OrdinalIgnoreCase))
        // {
        //     return Results.BadRequest("User is not a user");
        // }
        var existingUser = await services.UserService.GetById(userId);

        if (existingUser is not null && existingUser.IsOnboarded)
        {
            return Results.Conflict($"User with ID {userId} has already been onboarded.");
        }

        var user = new UserDto()
        {
            Id = userId,
            Email = request.Email,
            FullName = request.FullName,
            IsOnboarded = true,
            PreferredPronoun = request.PreferredPronoun,
            StreetAddress = request.StreetAddress,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode,
            Country = request.Country
        };

        if (existingUser is not null && existingUser.Roles != null && existingUser.Roles.Count > 0)
        {
            user.Roles = existingUser.Roles;
        }
        else
        {
            // Assign the role provided in the request
            user.Roles.Add("user");
        }

        // Update auth0
        await services.RoleManagementService.UpdateUserRoles(userId, user.Roles);

        // Create or update user profile
        await services.UserProfileService.CreateOrUpdate(
            userId,
            doB: request.DoB,
            preferredPronoun: request.PreferredPronoun,
            neurodiverseTraits: request.NeurodiverseTraits,
            preferences: request.Preferences);

        if (existingUser is not null)
        {
            // Update existing user
            var result = await services.UserService.Update(user);
            return Results.Ok(result);
        }
        else
        {
            // Create new user
            var result = await services.UserService.Create(user);
            return Results.Ok(result);
        }
    }

    private static async Task<IResult> OnboardCoach([AsParameters] UserServices services, CreateCoachRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }
        // if (!string.Equals(request.Role, "coach", StringComparison.OrdinalIgnoreCase))
        // {
        //     return Results.BadRequest("User is not a coach");
        // }

        var existingUser = await services.UserService.GetById(userId);
        if (existingUser is not null && existingUser.IsOnboarded)
        {
            return Results.Conflict($"User with ID {userId} has already been onboarded.");
        }

        var user = new UserDto()
        {
            Id = userId,
            Email = request.Email,
            FullName = request.FullName,
            IsOnboarded = true,
            StreetAddress = request.StreetAddress,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode,
            Country = request.Country
        };

        if (existingUser is not null && existingUser.Roles != null && existingUser.Roles.Count > 0)
        {
            user.Roles = existingUser.Roles;
        }
        else
        {
            // Assign the role provided in the request
            user.Roles.Add("coach");
        }

        // Update auth0
        await services.RoleManagementService.UpdateUserRoles(userId, user.Roles);

        // Create or update coach profile
        await services.CoachProfileService.CreateOrUpdate(
            userId,
            qualifications: request.Qualifications,
            specialisms: request.Specialisms,
            ageGroups: request.AgeGroups);

        if (existingUser is not null)
        {
            // Update existing user
            var result = await services.UserService.Update(user);
            return Results.Ok(result);
        }
        else
        {
            // Create new user
            var result = await services.UserService.Create(user);
            return Results.Ok(result);
        }
    }


    private static async Task<IResult> GetCurrentUser([AsParameters] UserServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var user = await services.UserService.GetById(userId);
        if (user is null)
        {
            return Results.NotFound();
        }

        // Check if user is a coach
        var isCoach = user.Roles?.Any(r => string.Equals(r, "coach", StringComparison.OrdinalIgnoreCase)) ?? false;

        if (isCoach)
        {
            // Return coach profile data
            var coachProfile = await services.CoachProfileService.GetByUserId(userId);
            if (coachProfile is null)
            {
                // User has coach role but no coach profile - return basic user data
                return Results.Ok(user);
            }

            var coachDto = coachProfile.ToCoachDto();

            // Set online status (30-minute window for coaches)
            await coachDto.SetOnlineStatusAsync(services.UserActivityService, activityWindowMinutes: 30);

            return Results.Ok(coachDto);
        }
        else
        {
            // Return user profile data
            var userProfile = await services.UserProfileService.GetByUserId(userId);
            if (userProfile is not null)
            {
                // Populate UserDto with profile data
                user.DoB = userProfile.DoB;
                user.PreferredPronoun = userProfile.PreferredPronoun;
                user.NeurodiverseTraits = userProfile.NeurodiverseTraits?.Select(t => t.Trait).ToList() ?? new List<string>();
                user.Preferences = userProfile.Preference?.Preferences;
            }

            return Results.Ok(user);
        }
    }

    private static async Task<IResult> UpdateUser([AsParameters] UserServices services, string userId, UpdateCurrentUserRequest request)
    {
        var loggedInUserId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(loggedInUserId))
        {
            return Results.Unauthorized();
        }

        // Validate that the userId in the URL matches the logged-in user
        if (!string.Equals(userId, loggedInUserId, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("You can only update your own user data.");
        }

        var existingUser = await services.UserService.GetById(userId);
        if (existingUser is null)
        {
            return Results.NotFound($"User with ID {userId} not found.");
        }

        // Update user data
        var user = new UserDto()
        {
            Id = userId,
            Email = existingUser.Email, // Email should not be changed via this endpoint
            FullName = request.FullName ?? existingUser.FullName,
            IsOnboarded = existingUser.IsOnboarded, // Don't allow changing onboarding status via this endpoint
            StreetAddress = request.StreetAddress ?? existingUser.StreetAddress,
            AddressLine2 = request.AddressLine2 ?? existingUser.AddressLine2,
            City = request.City ?? existingUser.City,
            StateProvince = request.StateProvince ?? existingUser.StateProvince,
            PostalCode = request.PostalCode ?? existingUser.PostalCode,
            Country = request.Country ?? existingUser.Country,
            Roles = existingUser.Roles // Preserve existing roles
        };

        // Update user in database
        var updatedUser = await services.UserService.Update(user);

        // Update user profile if profile fields are provided
        if (request.DoB.HasValue || request.PreferredPronoun != null ||
            request.NeurodiverseTraits != null || request.Preferences != null)
        {
            await services.UserProfileService.CreateOrUpdate(
                userId,
                doB: request.DoB,
                preferredPronoun: request.PreferredPronoun,
                neurodiverseTraits: request.NeurodiverseTraits,
                preferences: request.Preferences);
        }

        // Return the updated user
        var result = await services.UserService.GetById(userId);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCurrentUserRoles([AsParameters] UserServices services)
    {
        var userId = services.IdentityService.UserId;
        var result = await services.UserService.GetById(userId!);
        return result is not null ? Results.Ok(result.Roles) : Results.NotFound();
    }

    private static async Task<IResult> GetUserByEmail([AsParameters] UserServices services, string email)
    {
        var result = await services.UserService.GetByEmail(email);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
}

public class OnboardUserRequest
{
    public required string Email { get; init; }
    public required string FullName { get; init; }

    // public required string Role { get; init; }

    // Address fields
    public string? StreetAddress { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? StateProvince { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
}

public class CreateUserRequest : OnboardUserRequest
{
    public DateOnly? DoB { get; init; }
    public string? PreferredPronoun { get; init; }
    public IEnumerable<string>? NeurodiverseTraits { get; init; }
    public string? Preferences { get; init; }
}

public class CreateCoachRequest : OnboardUserRequest
{
    public required List<string> Qualifications { get; init; }
    public required List<string> Specialisms { get; init; }
    public required List<string> AgeGroups { get; init; }
}

public class UpdateCurrentUserRequest
{
    public string? FullName { get; init; }

    // Address fields
    public string? StreetAddress { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? StateProvince { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }

    // User profile fields
    public DateOnly? DoB { get; init; }
    public string? PreferredPronoun { get; init; }
    public IEnumerable<string>? NeurodiverseTraits { get; init; }
    public string? Preferences { get; init; }
}

public class Auth0Role
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}