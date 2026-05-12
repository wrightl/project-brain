using System.Text.Json;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.Extensions.Caching.Memory;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Authentication;

public class Auth0UserManagementServices(
    ILogger<Auth0UserManagementServices> logger,
    IMemoryCache memoryCache,
    IConfiguration configuration)
{
    public ILogger<Auth0UserManagementServices> Logger { get; } = logger;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IConfiguration Configuration { get; } = configuration;
}
public interface IAuth0UserManagement
{
    Task<string?> CreateUser(string email, string password, string fullName, string connection, bool emailVerified);
    Task<bool> UpdateUserRoles(string userId, List<string> roles);
    Task<bool> UpdateUser(string userId, BaseUserDto user);
    Task<bool> DeleteUserById(string id);
    Task<string?> GetUserIdByEmail(string email);
}
public class Auth0UserManagement : IAuth0UserManagement
{
    private readonly Auth0UserManagementServices _services;
    public Auth0UserManagement(Auth0UserManagementServices services)
    {
        _services = services;
    }

    public async Task<string?> CreateUser(string email, string password, string fullName, string connection, bool emailVerified)
    {
        var token = await getAuth0Token();
        var client = new HttpClient();

        // Create user payload for Auth0 Management API
        // Auth0 expects snake_case in the request
        var userPayload = new Dictionary<string, object>
        {
            { "email", email },
            { "password", password },
            { "name", fullName },
            { "connection", connection },
            { "email_verified", emailVerified }
        };

        var userJson = JsonSerializer.Serialize(userPayload);

        try
        {
            var config = _services.Configuration.GetSection("Auth0");
            var domain = config["Domain"];
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{domain}/api/v2/users");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = new StringContent(userJson, null, "application/json");

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                // Auth0 returns snake_case in responses
                var createdUser = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Auth0 returns "user_id" in snake_case
                if (createdUser.TryGetProperty("user_id", out var userIdElement))
                {
                    var userId = userIdElement.GetString();
                    _services.Logger.LogInformation("Created user in Auth0 with ID: {UserId}", userId);
                    return userId;
                }
                else
                {
                    _services.Logger.LogError("Auth0 user creation response missing user_id. Response: {ResponseContent}", responseContent);
                    return null;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _services.Logger.LogError("Failed to create user in Auth0. Status: {StatusCode}, Response: {ErrorContent}", response.StatusCode, errorContent);

                // If user already exists (409 Conflict), try to get the user by email
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    _services.Logger.LogWarning("User already exists in Auth0, attempting to retrieve by email: {Email}", email);
                    return await getUserIdByEmail(email, token, client);
                }

                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _services.Logger.LogError(ex, "HTTP exception while creating user in Auth0");
            throw;
        }
        catch (Exception ex)
        {
            _services.Logger.LogError(ex, "Exception while creating user in Auth0");
            throw;
        }
    }

    public async Task<string?> GetUserIdByEmail(string email)
    {
        var token = await getAuth0Token();
        var client = new HttpClient();
        return await getUserIdByEmail(email, token, client);
    }

    private async Task<string?> getUserIdByEmail(string email, string token, HttpClient client)
    {
        var response = await getResponse($"/users-by-email?email={Uri.EscapeDataString(email)}", token, client, HttpMethod.Get);

        var responseContent = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<JsonElement[]>(responseContent);

        if (users != null && users.Length > 0)
        {
            var firstUser = users[0];
            if (firstUser.TryGetProperty("user_id", out var userIdElement))
            {
                var userId = userIdElement.GetString();
                _services.Logger.LogInformation("Found existing user in Auth0 with ID: {UserId}", userId);
                return userId;
            }
        }

        return null;
    }

    public async Task<bool> UpdateUser(string userId, BaseUserDto user)
    {
        var token = await getAuth0Token();

        var client = new HttpClient();

        var userResponse = await getResponse($"/users/{userId}", token, client, HttpMethod.Get);

        if (userResponse.IsSuccessStatusCode)
        {
            var jsonStringUser = await userResponse.Content.ReadAsStringAsync();
            var auth0User = Auth0UserDto.FromJson(jsonStringUser);

            // Auth0 only allows PATCHing root attributes (name, email, nickname) on
            // Database connection users. For social/federated users (google-oauth2,
            // apple, windowslive, etc.) these are owned by the upstream IdP and Auth0
            // returns 400 Bad Request. Skip the PATCH in that case; user details are
            // still persisted locally via UserService.
            if (!isDatabaseConnection(auth0User))
            {
                _services.Logger.LogInformation(
                    "Skipping Auth0 PATCH for user {userId} on non-Database connection {connection}",
                    userId,
                    auth0User.Identities.FirstOrDefault()?.Connection ?? "(unknown)");
                return true;
            }

            if (Auth0UserPatchRequest.PatchableFieldsEqual(auth0User, user))
            {
                _services.Logger.LogInformation("No changes to user {userId} in Auth0", userId);
                return true;
            }

            var patchRequest = Auth0UserPatchRequest.FromAuth0UserAndApply(auth0User, user);
            var userJson = Auth0UserPatchRequest.ToJson(patchRequest);
            var result = await getResponse($"/users/{userId}", token, client, HttpMethod.Patch, new StringContent(userJson, null, "application/json"));
            return result.IsSuccessStatusCode;
        }
        else
        {
            _services.Logger.LogError("Failed to get user {userId} from Auth0", userId);
            return false;
        }
    }

    private static bool isDatabaseConnection(Auth0UserDto auth0User)
    {
        var connection = auth0User.Identities.FirstOrDefault()?.Connection;
        if (string.IsNullOrEmpty(connection))
        {
            // Fall back to the user_id prefix: Database users are "auth0|...".
            return auth0User.Id?.StartsWith("auth0|", StringComparison.Ordinal) ?? false;
        }
        // Auth0's "auth0" strategy is the Database connection. The default tenant
        // connection name is "Username-Password-Authentication" but custom DB
        // connections also report provider "auth0"; using the identity's provider
        // would be more accurate, but the connection name suffices for our setup.
        return string.Equals(connection, "Username-Password-Authentication", StringComparison.Ordinal);
    }

    // Keep the original method for backward compatibility with existing code
    public async Task<bool> UpdateUser(string userId, string userJson)
    {
        // Parse the JSON to check if update is needed
        var user = JsonSerializer.Deserialize<BaseUserDto>(userJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (user == null)
        {
            _services.Logger.LogError("Invalid user JSON provided for update");
            return false;
        }
        return await UpdateUser(userId, user);
    }

    public async Task<bool> UpdateUserRoles(string userId, List<string> roles)
    {
        var token = await getAuth0Token();

        // Get roles
        var client = new HttpClient();

        // Get roles from Auth0
        var roleResponse = await getResponse("/roles", token, client, HttpMethod.Get);
        var jsonString = await roleResponse.Content.ReadAsStringAsync();
        List<Auth0Role> listOfRoles = JsonSerializer.Deserialize<List<Auth0Role>>(jsonString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }) ?? new List<Auth0Role>();


        var usersRolesResponse = await getResponse($"/users/{userId}/roles", token, client, HttpMethod.Get);
        var jsonStringUserRoles = await usersRolesResponse.Content.ReadAsStringAsync();
        List<Auth0Role> listOfUsersRoles = JsonSerializer.Deserialize<List<Auth0Role>>(jsonStringUserRoles, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }) ?? new List<Auth0Role>();

        _services.Logger.LogInformation("Roles for user {userId}: {roleList}", userId, JsonSerializer.Serialize(listOfUsersRoles));

        List<string> roleIdsToAssign = new List<string>();
        List<string> roleIdsToRemove = new List<string>();

        foreach (var role in listOfRoles)
        {
            var isInUsersCurrentList = listOfUsersRoles.FirstOrDefault(r => r.Name == role.Name) != null;
            var isInNewList = roles.Contains(role.Name);

            if (!isInUsersCurrentList && isInNewList)
            {
                roleIdsToAssign.Add(role.Id);
            }
            else if (isInUsersCurrentList && !isInNewList)
            {
                roleIdsToRemove.Add(role.Id);
            }
        }

        if (roleIdsToRemove.Count > 0)
        {
            var roleIdsStringToRemove = JsonSerializer.Serialize(roleIdsToRemove);
            _services.Logger.LogInformation("Removing roles from user {roleIdsStringToRemove}", roleIdsStringToRemove);

            await getResponse($"/users/{userId}/roles", token, client, HttpMethod.Delete, new StringContent("{\"roles\":" + roleIdsStringToRemove + "}", null, "application/json"));
        }

        if (roleIdsToAssign.Count > 0)
        {
            var roleIdsStringToAssign = JsonSerializer.Serialize(roleIdsToAssign);
            _services.Logger.LogInformation("Assigning roles to user {roleIdsStringToAssign}", roleIdsStringToAssign);

            await getResponse($"/users/{userId}/roles", token, client, HttpMethod.Post, new StringContent("{\"roles\":" + roleIdsStringToAssign + "}", null, "application/json"));
        }

        return true;
    }

    public async Task<bool> DeleteUserById(string id)
    {
        var token = await getAuth0Token();
        var client = new HttpClient();
        var response = await getResponse($"/users/{id}", token, client, HttpMethod.Delete);
        return response.IsSuccessStatusCode;
    }

    private async Task<HttpResponseMessage> getResponse(string url, string token, HttpClient client, HttpMethod method, HttpContent? content = null)
    {
        var config = _services.Configuration.GetSection("Auth0");
        var domain = config["Domain"];
        var request = new HttpRequestMessage(method, $"https://{domain}/api/v2{url}");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Authorization", $"Bearer {token}");
        if (content != null)
        {
            request.Content = content;
        }
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<string> getAuth0Token()
    {
        var config = _services.Configuration.GetSection("Auth0");

        var cache = _services.MemoryCache;

        // Check if we have a valid, non-expired token in the cache
        if (cache.TryGetValue("Auth0ManagementApiToken", out string? token) && !string.IsNullOrEmpty(token))
        {
            return token;
        }

        var domain = config["Domain"] ?? throw new InvalidOperationException("Auth0 Domain is not configured");
        var clientId = config["ManagementApiClientId"];
        var clientSecret = config["ManagementApiClientSecret"];

        var authClient = new AuthenticationApiClient(domain);

        // Fetch the access token using the Client Credentials.
        var accessTokenResponse = await authClient.GetTokenAsync(new ClientCredentialsTokenRequest()
        {
            Audience = $"https://{domain}/api/v2/",
            ClientId = clientId ?? throw new InvalidOperationException("Auth0 ManagementApiClientId is not configured"),
            ClientSecret = clientSecret ?? throw new InvalidOperationException("Auth0 ManagementApiClientSecret is not configured"),
        });

        // Cache the new token, setting its expiration to 5 minutes before it *actually* expires
        cache.Set(
            "Auth0ManagementApiToken",
            accessTokenResponse.AccessToken,
            TimeSpan.FromSeconds(accessTokenResponse.ExpiresIn - 300)
        );

        return accessTokenResponse.AccessToken;
    }
}