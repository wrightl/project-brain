using System.Text.Json;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ProjectBrain.Api.Authentication;

public class RoleManagementServices(
    ILogger<RoleManagementServices> logger,
    IMemoryCache memoryCache,
    IConfiguration configuration)
{
    public ILogger<RoleManagementServices> Logger { get; } = logger;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IConfiguration Configuration { get; } = configuration;
}

public interface IRoleManagement
{
    Task UpdateUserRoles(string userId, List<string> roles);
}

public class Auth0RoleManagement : IRoleManagement
{
    private readonly RoleManagementServices _services;
    public Auth0RoleManagement(RoleManagementServices services)
    {
        _services = services;
    }

    public async Task UpdateUserRoles(string userId, List<string> roles)
    {
        var config = _services.Configuration.GetSection("Auth0");
        var domain = config["Domain"];

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
        var domain = config["Domain"] ?? throw new InvalidOperationException("Auth0 Domain is not configured");
        var clientId = config["ManagementApiClientId"];
        var clientSecret = config["ManagementApiClientSecret"];

        var cache = _services.MemoryCache;

        // Check if we have a valid, non-expired token in the cache
        if (cache.TryGetValue("Auth0ManagementApiToken", out string? token) && !string.IsNullOrEmpty(token))
        {
            return token;
        }

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