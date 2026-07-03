using System.Net.Http.Headers;
using System.Text.Json;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Polly.Registry;
using ProjectBrain.Auth;
using ProjectBrain.Domain;

namespace ProjectBrain.Auth.Auth0;

internal class Auth0UserManagementServices(
    ILogger<Auth0UserManagementServices> logger,
    IMemoryCache memoryCache,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ResiliencePipelineProvider<string> pipelineProvider)
{
    public ILogger<Auth0UserManagementServices> Logger { get; } = logger;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IConfiguration Configuration { get; } = configuration;
    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    public ResiliencePipelineProvider<string> PipelineProvider { get; } = pipelineProvider;
}

internal class Auth0UserManagement : IUserManagement
{
    private static readonly TimeSpan RolesCacheDuration = TimeSpan.FromHours(1);
    private static readonly JsonSerializerOptions RoleJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly Auth0UserManagementServices _services;

    public Auth0UserManagement(Auth0UserManagementServices services)
    {
        _services = services;
    }

    public async Task<string?> CreateUser(string email, string password, string fullName, string connection, bool emailVerified)
    {
        var token = await getAuth0Token();

        var userPayload = new Dictionary<string, object>
        {
            { "email", email },
            { "password", password },
            { "name", fullName },
            { "connection", connection },
            { "email_verified", emailVerified },
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

            var response = await sendAuth0RequestAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var createdUser = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (createdUser.TryGetProperty("user_id", out var userIdElement))
                {
                    var userId = userIdElement.GetString();
                    _services.Logger.LogInformation("Created user in Auth0 with ID: {UserId}", userId);
                    return userId;
                }

                _services.Logger.LogError("Auth0 user creation response missing user_id. Response: {ResponseContent}", responseContent);
                return null;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _services.Logger.LogError("Failed to create user in Auth0. Status: {StatusCode}, Response: {ErrorContent}", response.StatusCode, errorContent);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _services.Logger.LogWarning("User already exists in Auth0, attempting to retrieve by email: {Email}", email);
                return await getUserIdByEmail(email, token);
            }

            return null;
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
        return await getUserIdByEmail(email, token);
    }

    private async Task<string?> getUserIdByEmail(string email, string token)
    {
        var response = await getResponse($"/users-by-email?email={Uri.EscapeDataString(email)}", token, HttpMethod.Get);

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

        var userResponse = await getResponse($"/users/{userId}", token, HttpMethod.Get);

        if (userResponse.IsSuccessStatusCode)
        {
            var jsonStringUser = await userResponse.Content.ReadAsStringAsync();
            var auth0User = Auth0UserDto.FromJson(jsonStringUser);

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
            var result = await getResponse($"/users/{userId}", token, HttpMethod.Patch, new StringContent(userJson, null, "application/json"));
            return result.IsSuccessStatusCode;
        }

        _services.Logger.LogError("Failed to get user {userId} from Auth0", userId);
        return false;
    }

    private static bool isDatabaseConnection(Auth0UserDto auth0User)
    {
        var connection = auth0User.Identities.FirstOrDefault()?.Connection;
        if (string.IsNullOrEmpty(connection))
        {
            return auth0User.Id?.StartsWith("auth0|", StringComparison.Ordinal) ?? false;
        }

        return string.Equals(connection, "Username-Password-Authentication", StringComparison.Ordinal);
    }

    public async Task<bool> UpdateUser(string userId, string userJson)
    {
        var user = JsonSerializer.Deserialize<BaseUserDto>(userJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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

        List<Auth0Role> listOfRoles = await getCachedRolesAsync(token);

        var usersRolesResponse = await getResponse($"/users/{userId}/roles", token, HttpMethod.Get);
        var jsonStringUserRoles = await usersRolesResponse.Content.ReadAsStringAsync();
        List<Auth0Role> listOfUsersRoles = JsonSerializer.Deserialize<List<Auth0Role>>(jsonStringUserRoles, RoleJsonOptions)
            ?? new List<Auth0Role>();

        _services.Logger.LogInformation("Roles for user {userId}: {roleList}", userId, JsonSerializer.Serialize(listOfUsersRoles));

        var updatePlan = BuildRoleUpdatePlan(listOfRoles, listOfUsersRoles, roles);
        if (updatePlan.MissingRoles.Count > 0)
        {
            _services.Logger.LogError(
                "Cannot update roles for user {userId}. Requested role(s) were not found in Auth0: {MissingRoles}",
                userId,
                string.Join(", ", updatePlan.MissingRoles));
            return false;
        }

        if (updatePlan.RoleIdsToRemove.Count > 0)
        {
            var roleIdsStringToRemove = JsonSerializer.Serialize(updatePlan.RoleIdsToRemove);
            _services.Logger.LogInformation("Removing roles from user {roleIdsStringToRemove}", roleIdsStringToRemove);

            await getResponse($"/users/{userId}/roles", token, HttpMethod.Delete, new StringContent("{\"roles\":" + roleIdsStringToRemove + "}", null, "application/json"));
        }

        if (updatePlan.RoleIdsToAssign.Count > 0)
        {
            var roleIdsStringToAssign = JsonSerializer.Serialize(updatePlan.RoleIdsToAssign);
            _services.Logger.LogInformation("Assigning roles to user {roleIdsStringToAssign}", roleIdsStringToAssign);

            await getResponse($"/users/{userId}/roles", token, HttpMethod.Post, new StringContent("{\"roles\":" + roleIdsStringToAssign + "}", null, "application/json"));
        }

        return true;
    }

    internal static Auth0RoleUpdatePlan BuildRoleUpdatePlan(
        IReadOnlyCollection<Auth0Role> availableRoles,
        IReadOnlyCollection<Auth0Role> currentRoles,
        IReadOnlyCollection<string> requestedRoles)
    {
        var requestedRoleNames = requestedRoles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.Ordinal);

        var availableRoleNames = availableRoles
            .Where(role => !string.IsNullOrWhiteSpace(role.Name))
            .Select(role => role.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missingRoles = requestedRoleNames
            .Where(role => !availableRoleNames.Contains(role))
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();

        if (missingRoles.Length > 0)
        {
            return new Auth0RoleUpdatePlan([], [], missingRoles);
        }

        var currentRoleNames = currentRoles
            .Where(role => !string.IsNullOrWhiteSpace(role.Name))
            .Select(role => role.Name)
            .ToHashSet(StringComparer.Ordinal);

        var roleIdsToAssign = availableRoles
            .Where(role => requestedRoleNames.Contains(role.Name) && !currentRoleNames.Contains(role.Name))
            .Select(role => role.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();

        var roleIdsToRemove = availableRoles
            .Where(role => currentRoleNames.Contains(role.Name) && !requestedRoleNames.Contains(role.Name))
            .Select(role => role.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();

        return new Auth0RoleUpdatePlan(roleIdsToAssign, roleIdsToRemove, missingRoles);
    }

    public async Task<bool> DeleteUserById(string id)
    {
        var token = await getAuth0Token();
        var response = await getResponse($"/users/{id}", token, HttpMethod.Delete);
        return response.IsSuccessStatusCode;
    }

    private async Task<List<Auth0Role>> getCachedRolesAsync(string token)
    {
        var domain = _services.Configuration.GetSection("Auth0")["Domain"]
            ?? throw new InvalidOperationException("Auth0 Domain is not configured");
        var cacheKey = BuildRolesCacheKey(domain);
        var cache = _services.MemoryCache;

        if (cache.TryGetValue(cacheKey, out List<Auth0Role>? cachedRoles) && cachedRoles is { Count: > 0 })
        {
            return cachedRoles;
        }

        var roleResponse = await getResponse("/roles", token, HttpMethod.Get);
        var jsonString = await roleResponse.Content.ReadAsStringAsync();
        var listOfRoles = JsonSerializer.Deserialize<List<Auth0Role>>(jsonString, RoleJsonOptions)
            ?? new List<Auth0Role>();

        cache.Set(cacheKey, listOfRoles, RolesCacheDuration);
        return listOfRoles;
    }

    internal static string BuildRolesCacheKey(string domain) => $"Auth0ManagementApiRoles:{domain}";

    private async Task<HttpResponseMessage> getResponse(string url, string token, HttpMethod method, HttpContent? content = null)
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

        var response = await sendAuth0RequestAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<HttpResponseMessage> sendAuth0RequestAsync(HttpRequestMessage requestTemplate, CancellationToken cancellationToken = default)
    {
        var buffered = await bufferRequestAsync(requestTemplate, cancellationToken);
        var pipeline = _services.PipelineProvider.GetPipeline<HttpResponseMessage>(Auth0ManagementHttp.PipelineName);
        var client = _services.HttpClientFactory.CreateClient(Auth0ManagementHttp.ClientName);

        var response = await pipeline.ExecuteAsync(
            async ct =>
            {
                using var request = cloneRequest(buffered);
                return await client.SendAsync(request, ct);
            },
            cancellationToken);

        return response;
    }

    private static async Task<BufferedAuth0Request> bufferRequestAsync(HttpRequestMessage template, CancellationToken cancellationToken)
    {
        byte[]? body = null;
        string? mediaType = null;
        string? charset = null;

        if (template.Content != null)
        {
            body = await template.Content.ReadAsByteArrayAsync(cancellationToken);
            mediaType = template.Content.Headers.ContentType?.MediaType;
            charset = template.Content.Headers.ContentType?.CharSet;
        }

        return new BufferedAuth0Request(
            template.Method,
            template.RequestUri ?? throw new InvalidOperationException("Auth0 request URI is required."),
            template.Headers,
            body,
            mediaType,
            charset);
    }

    private static HttpRequestMessage cloneRequest(BufferedAuth0Request buffered)
    {
        var request = new HttpRequestMessage(buffered.Method, buffered.Uri);
        foreach (var header in buffered.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (buffered.Body != null)
        {
            var content = new ByteArrayContent(buffered.Body);
            if (buffered.MediaType != null)
            {
                content.Headers.ContentType = string.IsNullOrEmpty(buffered.Charset)
                    ? new MediaTypeHeaderValue(buffered.MediaType)
                    : new MediaTypeHeaderValue(buffered.MediaType) { CharSet = buffered.Charset };
            }

            request.Content = content;
        }

        return request;
    }

    private async Task<string> getAuth0Token()
    {
        var config = _services.Configuration.GetSection("Auth0");
        var cache = _services.MemoryCache;

        if (cache.TryGetValue("Auth0ManagementApiToken", out string? token) && !string.IsNullOrEmpty(token))
        {
            return token;
        }

        var domain = config["Domain"] ?? throw new InvalidOperationException("Auth0 Domain is not configured");
        var clientId = config["ManagementApiClientId"];
        var clientSecret = config["ManagementApiClientSecret"];

        var authClient = new AuthenticationApiClient(domain);

        var accessTokenResponse = await authClient.GetTokenAsync(new ClientCredentialsTokenRequest()
        {
            Audience = $"https://{domain}/api/v2/",
            ClientId = clientId ?? throw new InvalidOperationException("Auth0 ManagementApiClientId is not configured"),
            ClientSecret = clientSecret ?? throw new InvalidOperationException("Auth0 ManagementApiClientSecret is not configured"),
        });

        cache.Set(
            "Auth0ManagementApiToken",
            accessTokenResponse.AccessToken,
            TimeSpan.FromSeconds(accessTokenResponse.ExpiresIn - 300));

        return accessTokenResponse.AccessToken;
    }

    private sealed record BufferedAuth0Request(
        HttpMethod Method,
        Uri Uri,
        HttpRequestHeaders Headers,
        byte[]? Body,
        string? MediaType,
        string? Charset);
}

internal sealed record Auth0RoleUpdatePlan(
    IReadOnlyList<string> RoleIdsToAssign,
    IReadOnlyList<string> RoleIdsToRemove,
    IReadOnlyList<string> MissingRoles);
