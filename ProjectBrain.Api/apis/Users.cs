
using Microsoft.Extensions.Caching.Memory;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class UserServices(
    ILogger<UserServices> logger,
    IIdentityService identityService,
    IUserService userService,
    IAuth0UserManagement auth0UserManagementService,
    IMemoryCache memoryCache,
    IFeatureFlagService featureFlagService,
    IConfiguration configuration,
    IGeocodingService geocodingService,
    ICoachProfileService coachProfileService,
    IUserProfileService userProfileService,
    IUserActivityService userActivityService,
    ICoachMessageService coachMessageService,
    IOnboardingDataService onboardingDataService,
    Storage storage)
{
    public ILogger<UserServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IUserService UserService { get; } = userService;
    public IAuth0UserManagement Auth0UserManagementService { get; } = auth0UserManagementService;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IConfiguration Configuration { get; } = configuration;
    public IFeatureFlagService FeatureFlagService { get; } = featureFlagService;
    public IGeocodingService GeocodingService { get; } = geocodingService;
    public ICoachProfileService CoachProfileService { get; } = coachProfileService;
    public IUserProfileService UserProfileService { get; } = userProfileService;
    public IUserActivityService UserActivityService { get; } = userActivityService;
    public ICoachMessageService CoachMessageService { get; } = coachMessageService;
    public IOnboardingDataService OnboardingDataService { get; } = onboardingDataService;
    public Storage Storage { get; } = storage;
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

        // Theme endpoints
        group.MapGet("/me/theme", GetTheme).WithName("GetTheme");
        group.MapPut("/me/theme", UpdateTheme).WithName("UpdateTheme");

        // Timezone endpoints (stored in UserPreference.Preferences JSON)
        group.MapGet("/me/timezone", GetTimezone).WithName("GetTimezone");
        group.MapPut("/me/timezone", UpdateTimezone).WithName("UpdateTimezone");

        if (app.Environment.IsDevelopment())
        {
            group.MapGet("/{email}", GetUserByEmail).WithName("GetUserByEmail");
        }
    }

    private static async Task<IResult> OnboardUser([AsParameters] UserServices services, CreateUserRequest request)
    {
        var userId = services.IdentityService.UserId!;

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
            Country = request.Country,
        };

        // Populate coordinates (only when we have a meaningful location)
        if (!string.IsNullOrWhiteSpace(user.City) && !string.IsNullOrWhiteSpace(user.Country))
        {
            var geocoded = await services.GeocodingService.GeocodeAsync(user.City, user.StateProvince, user.Country);
            user.Latitude = geocoded?.Latitude;
            user.Longitude = geocoded?.Longitude;
        }

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
        await services.Auth0UserManagementService.UpdateUserRoles(userId, user.Roles);
        await services.Auth0UserManagementService.UpdateUser(userId, user);

        // Create or update user FIRST (before UserProfile to satisfy foreign key constraint)
        BaseUserDto createdUser;
        if (existingUser is not null)
        {
            // Update existing user
            createdUser = await services.UserService.Update(user);
        }
        else
        {
            // Create new user
            createdUser = await services.UserService.Create(user);
        }

        // Create or update user profile (now that User exists)
        var userProfile = await services.UserProfileService.CreateOrUpdate(
            userId,
            doB: request.DoB,
            preferredPronoun: request.PreferredPronoun,
            neurodiverseTraits: request.NeurodiverseTraits,
            preferences: GetPreferencesObject(request.Preferences));

        // Save onboarding data to database if provided
        if (request.Onboarding != null)
        {
            try
            {
                await services.OnboardingDataService.CreateOrUpdate(userId, request.Onboarding);
                services.Logger.LogInformation("Successfully saved onboarding data to database for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the onboarding process if database save fails
                services.Logger.LogError(ex, "Failed to save onboarding data to database for user {UserId}", userId);
            }
        }

        // Build and upload onboarding as markdown (for AI Chat/Agent) with indexing
        try
        {
            var onboardingData = CreateOnboardingData(userProfile, createdUser as UserDto, request.Onboarding);
            var markdown = BuildOnboardingMarkdown(onboardingData);
            var mdFilename = Constants.ONBOARDING_MARKDOWN_FILENAME;
            var mdBytes = Encoding.UTF8.GetBytes(markdown);
            await using (var mdStream = new MemoryStream(mdBytes))
            {
                var options = new StorageUploadOptions
                {
                    UserId = userId,
                    FileOwnership = FileOwnership.User,
                    StorageType = StorageType.Onboarding,
                    ResourceId = mdFilename,
                    SkipIndexing = false
                };
                await services.Storage.UploadFile(mdStream, mdFilename, options);
            }
            services.Logger.LogInformation("Successfully uploaded onboarding markdown to blob storage for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Failed to upload onboarding markdown for user {UserId}", userId);
        }

        return Results.Ok(createdUser);
    }

    public static object CreateOnboardingData(UserProfile userProfile, UserDto createdUser, object? structuredOnboardingData = null)
    {
        var baseData = new Dictionary<string, object?>
        {
            ["doB"] = userProfile.DoB,
            ["fullName"] = createdUser.FullName,
            ["email"] = createdUser.Email,
            ["preferredPronoun"] = userProfile.PreferredPronoun,
            ["neurodiverseTraits"] = userProfile.NeurodiverseTraits?.Select(t => t.Trait).ToList() ?? new List<string>(),
            ["preferences"] = userProfile.Preference?.Preferences,
            ["streetAddress"] = createdUser.StreetAddress,
            ["addressLine2"] = createdUser.AddressLine2,
            ["city"] = createdUser.City,
            ["stateProvince"] = createdUser.StateProvince,
            ["postalCode"] = createdUser.PostalCode,
            ["country"] = createdUser.Country
        };

        // Store the full onboarding object under one key so profile "preferences" is not overwritten
        if (structuredOnboardingData != null)
        {
            var structuredJson = JsonSerializer.Serialize(structuredOnboardingData);
            using var structuredDoc = JsonDocument.Parse(structuredJson);
            baseData["onboarding"] = ConvertJsonElementToObject(structuredDoc.RootElement);
        }

        return baseData;
    }

    /// <summary>Builds markdown with clear sections for AI consumption (Chat/Agent).</summary>
    private static string BuildOnboardingMarkdown(object onboardingData)
    {
        if (onboardingData is not Dictionary<string, object?> data)
            return "# Onboarding\n\nNo data.";

        var sb = new StringBuilder();
        sb.AppendLine("# Onboarding – User profile");
        sb.AppendLine();

        // Base profile fields
        AppendSectionIfPresent(sb, "Full name", data, "fullName");
        AppendSectionIfPresent(sb, "Email", data, "email");
        AppendSectionIfPresent(sb, "Date of birth", data, "doB");
        AppendSectionIfPresent(sb, "Preferred pronoun", data, "preferredPronoun");

        if (data.TryGetValue("neurodiverseTraits", out var traits) && traits is IEnumerable<object> traitList && traitList.Any())
        {
            sb.AppendLine("## Neurodiverse traits");
            foreach (var t in traitList)
                sb.AppendLine("- " + (t?.ToString() ?? ""));
            sb.AppendLine();
        }

        if (data.TryGetValue("preferences", out var prefs) && prefs != null)
        {
            sb.AppendLine("## Preferences");
            AppendValue(sb, prefs);
            sb.AppendLine();
        }

        sb.AppendLine("## Location");
        var hasLocation = false;
        if (data.TryGetValue("streetAddress", out var addr) && addr != null && !string.IsNullOrWhiteSpace(addr.ToString())) { sb.AppendLine("- **Street:** " + addr); hasLocation = true; }
        if (data.TryGetValue("addressLine2", out var addr2) && addr2 != null && !string.IsNullOrWhiteSpace(addr2.ToString())) { sb.AppendLine("- **Address line 2:** " + addr2); hasLocation = true; }
        if (data.TryGetValue("city", out var city) && city != null && !string.IsNullOrWhiteSpace(city.ToString())) { sb.AppendLine("- **City:** " + city); hasLocation = true; }
        if (data.TryGetValue("stateProvince", out var state) && state != null && !string.IsNullOrWhiteSpace(state.ToString())) { sb.AppendLine("- **State/Province:** " + state); hasLocation = true; }
        if (data.TryGetValue("postalCode", out var postal) && postal != null && !string.IsNullOrWhiteSpace(postal.ToString())) { sb.AppendLine("- **Postal code:** " + postal); hasLocation = true; }
        if (data.TryGetValue("country", out var country) && country != null && !string.IsNullOrWhiteSpace(country.ToString())) { sb.AppendLine("- **Country:** " + country); hasLocation = true; }
        if (!hasLocation)
            sb.AppendLine("Not provided.");
        sb.AppendLine();

        // Structured onboarding sections (wizard + follow-on)
        if (data.TryGetValue("onboarding", out var onboardingObj) && onboardingObj is Dictionary<string, object?> onboarding)
        {
            AppendSectionIfPresent(sb, "Locale", onboarding, "locale");
            AppendOnboardingSection(sb, "Welcome", onboarding, "welcome");
            AppendOnboardingSection(sb, "About you", onboarding, "aboutYou");
            AppendOnboardingSection(sb, "Preferences (onboarding)", onboarding, "preferences");
            AppendOnboardingSection(sb, "Profile", onboarding, "profile");
            AppendOnboardingSection(sb, "Coaching buddy", onboarding, "coachingBuddy");
            AppendOnboardingSection(sb, "Closing", onboarding, "closing");

            if (onboarding.TryGetValue("followOnQuestions", out var followOn) && followOn is Dictionary<string, object?> followOnDict && followOnDict.Count > 0)
            {
                sb.AppendLine("## Follow-on questions");
                sb.AppendLine();
                foreach (var kv in followOnDict.OrderBy(k => k.Key))
                {
                    var categoryTitle = ToTitleCase(kv.Key);
                    if (kv.Value is Dictionary<string, object?> categoryDict && categoryDict.Count > 0)
                    {
                        sb.AppendLine("### " + categoryTitle);
                        AppendKeyValueBlock(sb, categoryDict);
                        sb.AppendLine();
                    }
                }
            }
        }

        return sb.ToString();
    }

    private static void AppendSectionIfPresent(StringBuilder sb, string title, Dictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value == null) return;
        var s = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(s) && value is not bool) return;
        sb.AppendLine("## " + title);
        AppendValue(sb, value);
        sb.AppendLine();
    }

    private static void AppendOnboardingSection(StringBuilder sb, string title, Dictionary<string, object?> onboarding, string key)
    {
        if (!onboarding.TryGetValue(key, out var value) || value == null) return;
        if (value is Dictionary<string, object?> dict && dict.Count == 0) return;
        if (value is string str && string.IsNullOrWhiteSpace(str)) return;
        sb.AppendLine("## " + title);
        AppendValue(sb, value);
        sb.AppendLine();
    }

    private static void AppendValue(StringBuilder sb, object? value)
    {
        if (value == null) return;
        if (value is bool b)
        {
            sb.AppendLine(b ? "Yes" : "No");
            return;
        }
        if (value is System.Collections.IList list && list.Count > 0)
        {
            foreach (var item in list)
                sb.AppendLine("- " + (item?.ToString() ?? ""));
            return;
        }
        if (value is Dictionary<string, object?> dict)
        {
            AppendKeyValueBlock(sb, dict);
            return;
        }
        var text = value.ToString()?.Trim();
        if (!string.IsNullOrEmpty(text))
            sb.AppendLine(text);
    }

    private static void AppendKeyValueBlock(StringBuilder sb, Dictionary<string, object?> dict)
    {
        foreach (var kv in dict.OrderBy(k => k.Key))
        {
            if (kv.Value == null) continue;
            var label = ToTitleCase(kv.Key);
            if (kv.Value is bool b)
            {
                sb.AppendLine("- **" + label + ":** " + (b ? "Yes" : "No"));
                continue;
            }
            if (kv.Value is System.Collections.IList list && list.Count > 0)
            {
                sb.AppendLine("- **" + label + ":**");
                foreach (var item in list)
                    sb.AppendLine("  - " + (item?.ToString() ?? ""));
                continue;
            }
            if (kv.Value is Dictionary<string, object?> nested && nested.Count > 0)
            {
                sb.AppendLine("- **" + label + ":**");
                foreach (var n in nested.OrderBy(k => k.Key))
                {
                    var v = n.Value;
                    if (v == null) continue;
                    var subLabel = ToTitleCase(n.Key);
                    if (v is bool vb)
                        sb.AppendLine("  - " + subLabel + ": " + (vb ? "Yes" : "No"));
                    else
                        sb.AppendLine("  - " + subLabel + ": " + (v.ToString()?.Trim() ?? ""));
                }
                continue;
            }
            var scalar = kv.Value.ToString()?.Trim();
            if (!string.IsNullOrEmpty(scalar))
                sb.AppendLine("- **" + label + ":** " + scalar);
        }
    }

    private static string ToTitleCase(string camelCase)
    {
        if (string.IsNullOrEmpty(camelCase)) return camelCase;
        var result = new StringBuilder();
        result.Append(char.ToUpperInvariant(camelCase[0]));
        for (var i = 1; i < camelCase.Length; i++)
        {
            if (char.IsUpper(camelCase[i]))
                result.Append(' ');
            result.Append(camelCase[i]);
        }
        return result.ToString();
    }

    private static object? ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : (object)element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToObject).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                p => p.Name,
                p => ConvertJsonElementToObject(p.Value)
            ),
            _ => element.GetRawText()
        };
    }

    private static async Task<IResult> OnboardCoach([AsParameters] UserServices services, CreateCoachRequest request)
    {
        var userId = services.IdentityService.UserId!;

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
            Country = request.Country,
        };

        // Populate coordinates (only when we have a meaningful location)
        if (!string.IsNullOrWhiteSpace(user.City) && !string.IsNullOrWhiteSpace(user.Country))
        {
            var geocoded = await services.GeocodingService.GeocodeAsync(user.City, user.StateProvince, user.Country);
            user.Latitude = geocoded?.Latitude;
            user.Longitude = geocoded?.Longitude;
        }

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
        await services.Auth0UserManagementService.UpdateUserRoles(userId, user.Roles);
        await services.Auth0UserManagementService.UpdateUser(userId, user);

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
        var userId = services.IdentityService.UserId!;

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
            await coachDto.SetOnlineStatusAsync(services.UserActivityService, services.CoachMessageService, activityWindowMinutes: 30);

            return Results.Ok(coachDto);
        }
        else
        {
            // Return user profile data
            var userProfile = await services.UserProfileService.GetByUserId(userId);

            if (userProfile is null)
            {
                // User has user role but no user profile - return basic user data
                return Results.Ok(user);
            }
            var userDto = userProfile.ToUserDto();

            return Results.Ok(userDto);
        }
    }

    private static async Task<IResult> UpdateUser([AsParameters] UserServices services, string userId, UpdateCurrentUserRequest request)
    {
        var loggedInUserId = services.IdentityService.UserId!;

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

        var locationChanged =
            !string.Equals(user.City, existingUser.City, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(user.StateProvince, existingUser.StateProvince, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(user.Country, existingUser.Country, StringComparison.OrdinalIgnoreCase);

        if (locationChanged)
        {
            if (!string.IsNullOrWhiteSpace(user.City) && !string.IsNullOrWhiteSpace(user.Country))
            {
                var geocoded = await services.GeocodingService.GeocodeAsync(user.City, user.StateProvince, user.Country);
                user.Latitude = geocoded?.Latitude;
                user.Longitude = geocoded?.Longitude;
            }
            else
            {
                user.Latitude = null;
                user.Longitude = null;
            }
        }
        else
        {
            user.Latitude = existingUser.Latitude;
            user.Longitude = existingUser.Longitude;
        }

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
                preferences: GetPreferencesObject(request.Preferences));
        }

        // Return the updated user
        var result = await services.UserService.GetById(userId);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCurrentUserRoles([AsParameters] UserServices services)
    {
        var userId = services.IdentityService.UserId!;
        var result = await services.UserService.GetById(userId);
        return result is not null ? Results.Ok(result.Roles) : Results.NotFound();
    }

    private static async Task<IResult> GetUserByEmail([AsParameters] UserServices services, string email)
    {
        var result = await services.UserService.GetByEmail(email);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> GetTheme([AsParameters] UserServices services)
    {
        var userId = services.IdentityService.UserId!;

        var user = await services.UserService.GetById(userId);
        if (user is null)
        {
            return Results.NotFound("User not found");
        }

        // Check if user is a coach
        var isCoach = user.Roles?.Any(r => string.Equals(r, "coach", StringComparison.OrdinalIgnoreCase)) ?? false;

        // Get user profile to access preferences
        var userProfile = await services.UserProfileService.GetByUserId(userId);
        if (userProfile is null)
        {
            return Results.Ok(new { theme = "standard" });
        }

        var preferences = userProfile.Preference?.Preferences;
        var theme = ParseThemeFromPreferences(preferences);

        return Results.Ok(new { theme });
    }

    private static async Task<IResult> UpdateTheme([AsParameters] UserServices services, UpdateThemeRequest request)
    {
        var userId = services.IdentityService.UserId!;

        // Validate theme value
        if (string.IsNullOrWhiteSpace(request.Theme))
        {
            return Results.BadRequest("Theme is required");
        }

        var validThemes = new[] { "standard", "dark", "colourful" };
        if (!validThemes.Contains(request.Theme, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest($"Invalid theme value. Must be one of: {string.Join(", ", validThemes)}");
        }

        var user = await services.UserService.GetById(userId);
        if (user is null)
        {
            return Results.NotFound("User not found");
        }

        // Get user profile to access preferences
        var userProfile = await services.UserProfileService.GetByUserId(userId);

        // Get existing preferences and merge with theme
        var preferencesObj = GetPreferencesObject(userProfile?.Preference?.Preferences);

        // Update theme in preferences
        preferencesObj["theme"] = request.Theme.ToLowerInvariant();

        // Update user profile with new preferences
        await services.UserProfileService.CreateOrUpdate(
            userId,
            preferences: preferencesObj);

        return Results.Ok(new { theme = request.Theme.ToLowerInvariant() });
    }

    private static async Task<IResult> GetTimezone([AsParameters] UserServices services)
    {
        var userId = services.IdentityService.UserId!;

        var userProfile = await services.UserProfileService.GetByUserId(userId);
        var preferences = userProfile?.Preference?.Preferences;
        var preferencesObj = GetPreferencesObject(preferences);

        if (preferencesObj.TryGetValue("timezone", out var timezoneObj) && timezoneObj is string timezoneStr)
        {
            return Results.Ok(new { timezone = timezoneStr });
        }

        return Results.Ok(new { timezone = (string?)null });
    }

    private static async Task<IResult> UpdateTimezone([AsParameters] UserServices services, UpdateTimezoneRequest request)
    {
        var userId = services.IdentityService.UserId!;

        if (string.IsNullOrWhiteSpace(request.Timezone))
        {
            return Results.BadRequest("Timezone is required");
        }

        // Update timezone in preferences (keep other preference keys)
        var userProfile = await services.UserProfileService.GetByUserId(userId);
        var preferencesObj = GetPreferencesObject(userProfile?.Preference?.Preferences);
        preferencesObj["timezone"] = request.Timezone.Trim();

        await services.UserProfileService.CreateOrUpdate(userId, preferences: preferencesObj);

        return Results.Ok(new { timezone = request.Timezone.Trim() });
    }

    private static Dictionary<string, object> GetPreferencesObject(string? preferences)
    {
        Dictionary<string, object> preferencesObj;
        if (preferences is not null)
        {
            try
            {
                // Try to parse as JSON object
                using var doc = JsonDocument.Parse(preferences);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    preferencesObj = new Dictionary<string, object>();
                    foreach (var prop in root.EnumerateObject())
                    {
                        // Convert JsonElement to appropriate .NET type
                        object? value = prop.Value.ValueKind switch
                        {
                            JsonValueKind.String => prop.Value.GetString(),
                            JsonValueKind.Number => prop.Value.TryGetInt32(out var intVal) ? intVal : (object)prop.Value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            _ => prop.Value.GetRawText()
                        };
                        preferencesObj[prop.Name] = value ?? string.Empty;
                    }
                }
                else
                {
                    // If not an object, wrap it
                    preferencesObj = new Dictionary<string, object> { ["other"] = preferences };
                }
            }
            catch
            {
                // If preferences is not JSON, keep it as is in a separate field
                preferencesObj = new Dictionary<string, object> { ["other"] = preferences };
            }
        }
        else
        {
            preferencesObj = new Dictionary<string, object>();
        }

        return preferencesObj;
    }

    private static string ParseThemeFromPreferences(string? preferences)
    {
        if (string.IsNullOrWhiteSpace(preferences))
        {
            return "standard";
        }

        try
        {
            using var doc = JsonDocument.Parse(preferences);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("theme", out var themeElement))
            {
                var theme = themeElement.GetString()?.ToLowerInvariant();
                var validThemes = new[] { "standard", "dark", "colourful" };
                if (theme is not null && validThemes.Contains(theme))
                {
                    return theme;
                }
            }
        }
        catch
        {
            // If parsing fails, check if it's a plain string
            var validThemes = new[] { "standard", "dark", "colourful" };
            if (validThemes.Contains(preferences.ToLowerInvariant()))
            {
                return preferences.ToLowerInvariant();
            }
        }

        return "standard";
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

    [JsonPropertyName("onboarding")]
    public object? Onboarding { get; init; }
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

public class UpdateThemeRequest
{
    public required string Theme { get; init; }
}

public class UpdateTimezoneRequest
{
    public required string Timezone { get; init; }
}