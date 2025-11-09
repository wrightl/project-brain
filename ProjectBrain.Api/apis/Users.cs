
using System.Text.Json;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.Extensions.Caching.Memory;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class UserServices(
    ILogger<UserServices> logger,
    IIdentityService identityService,
    IUserService userService,
    IMemoryCache memoryCache,
    IConfiguration configuration)
{
    public ILogger<UserServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IUserService UserService { get; } = userService;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IConfiguration Configuration { get; } = configuration;
}

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("users").RequireAuthorization();

        group.MapPost("/me/onboarding", OnboardUser);
        group.MapGet("/me", GetCurrentUser).WithName("GetCurrentUser");
        group.MapGet("/roles", GetCurrentUserRoles).WithName("GetCurrentUserRoles");
        group.MapDelete("/{id}", DeleteUser).WithName("DeleteUser");

        if (app.Environment.IsDevelopment())
        {
            group.MapGet("/config", GetConfig).WithName("GetConfig").AllowAnonymous();
            group.MapGet("/{email}", GetUserByEmail).WithName("GetUserByEmail");
        }
    }

    private static async Task<IResult> GetConfig([AsParameters] UserServices services)
    {
        var config = services.Configuration.GetSection("Auth0");

        var values = new
        {
            Auth0Domain = config["Domain"],
            Auth0ClientId = config["ClientId"],
            Auth0ManagementApiClientSecret = config["ManagementApiClientSecret"],
            Auth0ManagementApiClientId = config["ManagementApiClientId"],
            Token = await getAuth0Token(services, config)
        };

        return Results.Ok(values);
    }

    private static async Task<IResult> OnboardUser([AsParameters] UserServices services, CreateUserRequest request)
    {
        var userId = services.IdentityService.UserId;
        var existingUser = await services.UserService.GetById(userId);
        _ = request.Role ?? throw new ArgumentNullException(nameof(request.Role));

        if (existingUser is not null && existingUser.IsOnboarded)
        {
            return Results.Conflict($"User with ID {userId} has already been onboarded.");
        }

        var user = new UserDto()
        {
            Id = userId,
            Email = request.Email,
            FullName = request.FullName,
            DoB = request.DoB,
            FavoriteColour = request.FavoriteColor,
            IsOnboarded = true,
            PreferredPronoun = request.PreferredPronoun,
            NeurodivergentDetails = request.NeurodivergentDetails,
            Address = request.Address,
            Experience = request.Experience
        };

        if (existingUser is not null && existingUser.Roles != null && existingUser.Roles.Count > 0)
        {
            user.Roles = existingUser.Roles;
        }
        else
        {
            // Assign the role provided in the request
            user.Roles.Add(request.Role);
        }

        // Update auth0
        await updateAuth0UserRole(services, userId, user.Roles);

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
        var result = await services.UserService.GetById(userId!);
        return result is not null ? Results.Ok(result) : Results.NotFound();
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

    // TODO: Needs protecting so only admins can delete users
    private static async Task<IResult> DeleteUser([AsParameters] UserServices services, string id)
    {
        var result = await services.UserService.DeleteById(id);

        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task updateAuth0UserRole(UserServices services, string userId, List<string> roles)
    {
        var config = services.Configuration.GetSection("Auth0");
        var domain = config["Domain"];

        var token = await getAuth0Token(services, config);

        // Get roles
        var client = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://{domain}/api/v2/roles");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Authorization", $"Bearer {token}");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var jsonString = await response.Content.ReadAsStringAsync();
        List<Auth0Role> roleList = JsonSerializer.Deserialize<List<Auth0Role>>(jsonString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }) ?? new List<Auth0Role>();


        List<string> roleIdsToAssign = new List<string>();
        foreach (var roleName in roles)
        {
            var role = roleList.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (role != null)
            {
                roleIdsToAssign.Add(role.Id);
            }
        }

        var roleIdsString = JsonSerializer.Serialize(roleIdsToAssign);
        services.Logger.LogInformation("Assigning roles to user {roleIdsString}", roleIdsString);

        var assignRoleRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{domain}/api/v2/users/{userId}/roles");
        assignRoleRequest.Headers.Add("Authorization", $"Bearer {token}");
        var content = new StringContent("{\"roles\":" + roleIdsString + "}", null, "application/json");
        assignRoleRequest.Content = content;
        var assignRoleResponse = await client.SendAsync(assignRoleRequest);
        assignRoleResponse.EnsureSuccessStatusCode();
    }

    private static async Task<string> getAuth0Token(UserServices services, IConfigurationSection config)
    {
        var domain = config["Domain"];
        var clientId = config["ManagementApiClientId"];
        var clientSecret = config["ManagementApiClientSecret"];

        var cache = services.MemoryCache;

        // Check if we have a valid, non-expired token in the cache
        if (cache.TryGetValue("Auth0ManagementApiToken", out string token))
        {
            return token;
        }

        var authClient = new AuthenticationApiClient(domain);

        services.Logger.LogInformation("Fetching new Auth0 Management API token");
        services.Logger.LogInformation("ClientId: {ClientId}", clientId);
        services.Logger.LogInformation("ClientSecret: {ClientSecret}", clientSecret != null ? "****" : "null");
        services.Logger.LogInformation("Audience: {Audience}", $"https://{domain}/api/v2/");

        // Fetch the access token using the Client Credentials.
        var accessTokenResponse = await authClient.GetTokenAsync(new ClientCredentialsTokenRequest()
        {
            Audience = $"https://{domain}/api/v2/",
            ClientId = clientId,
            ClientSecret = clientSecret,
        });

        services.Logger.LogInformation("Access Token Response: {AccessTokenResponse}", accessTokenResponse.AccessToken);
        services.Logger.LogInformation("Received Auth0 Management API token, expires in {ExpiresIn} seconds", accessTokenResponse.ExpiresIn);

        // Cache the new token, setting its expiration to 5 minutes before it *actually* expires
        cache.Set(
            "Auth0ManagementApiToken",
            accessTokenResponse.AccessToken,
            TimeSpan.FromSeconds(accessTokenResponse.ExpiresIn - 300)
        );

        return accessTokenResponse.AccessToken;
    }
}

public class CreateUserRequest
{
    public required string Email { get; init; }
    public required string FullName { get; init; }

    public required DateOnly DoB { get; init; }
    public required string FavoriteColor { get; init; }

    public required string Role { get; init; }

    // User-specific fields
    public string? PreferredPronoun { get; init; }
    public string? NeurodivergentDetails { get; init; }

    // Coach-specific fields
    public string? Address { get; init; }
    public string? Experience { get; init; }
}

public class Auth0Role
{
    public string Id { get; set; }
    public string Name { get; set; }
}