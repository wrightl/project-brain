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
    Task<bool> UpdateUserRoles(string userId, List<string> roles);
    Task<bool> UpdateUser(string userId, BaseUserDto user);
    Task<bool> DeleteUserById(string id);
}

public class Auth0UserManagement : IAuth0UserManagement
{
    private readonly Auth0UserManagementServices _services;
    public Auth0UserManagement(Auth0UserManagementServices services)
    {
        _services = services;
    }

    public async Task<bool> UpdateUser(string userId, BaseUserDto user)
    {
        var token = await getAuth0Token();

        var client = new HttpClient();

        var userResponse = await getResponse($"/users/{userId}", token, client, HttpMethod.Get);

        if (userResponse.IsSuccessStatusCode)
        {
            var jsonStringUser = await userResponse.Content.ReadAsStringAsync();
            var auth0User = BaseUserDto.FromJson(jsonStringUser);

            // Only update auth0 if any user details have changed
            if (BaseUserDto.Equals(auth0User, user))
            {
                _services.Logger.LogInformation("No changes to user {userId} in Auth0", userId);
                return true;
            }

            var userJson = BaseUserDto.ToJson(user);

            var result = await getResponse($"/users/{userId}", token, client, HttpMethod.Patch, new StringContent(userJson, null, "application/json"));
            return result.IsSuccessStatusCode;
        }
        else
        {
            _services.Logger.LogError("Failed to get user {userId} from Auth0", userId);
            return false;
        }
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