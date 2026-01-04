using System.Text;
using System.Text.Json;
using ProjectBrain.Domain;

public class Auth0WebhookServices(
        ILogger<Auth0WebhookServices> logger,
        IUserService userService,
        IEmailService emailService,
        HttpContext context,
        IConfiguration configuration,
        Storage storage)
{
    public ILogger<Auth0WebhookServices> Logger { get; } = logger;
    public IUserService UserService { get; } = userService;
    public HttpContext Context { get; } = context;
    public IConfiguration Configuration { get; } = configuration;
    public IEmailService EmailService { get; } = emailService;
    public Storage Storage { get; } = storage;
}

/// <summary>
/// Represents user data extracted from Auth0 webhook payload
/// </summary>
public class Auth0UserData
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Connection { get; init; }
    public bool EmailVerified { get; init; } = false;
}

public static class Auth0WebhookEndpoints
{
    public static void MapAuth0WebhookEndpoints(this WebApplication app)
    {
        // Auth0 webhook endpoint - no authorization required (uses bearer token verification)
        app.MapPost("/webhooks/auth0", HandleAuth0Webhook)
            .WithName("Auth0Webhook")
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleAuth0Webhook(
        [AsParameters] Auth0WebhookServices services)
    {
        // Read request body
        var requestBody = await new StreamReader(services.Context.Request.Body, Encoding.UTF8).ReadToEndAsync();

        if (string.IsNullOrEmpty(requestBody))
        {
            services.Logger.LogWarning("Received empty Auth0 webhook request");
            return Results.BadRequest("Empty request body");
        }

        // Extract bearer token from Authorization header
        var authHeader = services.Context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            services.Logger.LogWarning("Auth0 webhook missing or invalid Authorization header");
            return Results.BadRequest("Missing or invalid Authorization header");
        }

        var providedToken = authHeader.Substring("Bearer ".Length).Trim();

        // Verify bearer token
        var expectedToken = services.Configuration["Auth0:WebhookToken"];

        if (string.IsNullOrEmpty(expectedToken))
        {
            services.Logger.LogError("Auth0:WebhookToken is not configured");
            return Results.BadRequest("Webhook token not configured");
        }

        if (!string.Equals(providedToken, expectedToken, StringComparison.Ordinal))
        {
            services.Logger.LogWarning("Invalid Auth0 webhook token");
            return Results.BadRequest("Invalid webhook token");
        }

        try
        {
            // Parse webhook payload
            using var jsonDoc = JsonDocument.Parse(requestBody);
            var root = jsonDoc.RootElement;

            // Extract event type
            if (!root.TryGetProperty("type", out var eventTypeElement))
            {
                services.Logger.LogWarning("Auth0 webhook missing 'type' field");
                return Results.BadRequest("Missing event type");
            }

            var eventType = eventTypeElement.GetString();
            services.Logger.LogInformation("Received Auth0 webhook: {EventType}", eventType);

            // Handle user.created event
            if (eventType == "user.created")
            {
                await HandleUserCreated(services, root);
            }
            else if (eventType == "user.updated")
            {
                await HandleUserUpdated(services, root);
            }
            else if (eventType == "user.deleted")
            {
                await HandleUserDeleted(services, root);
            }
            else
            {
                services.Logger.LogInformation("Unhandled Auth0 event type: {EventType}", eventType);
            }

            return Results.Ok();
        }
        catch (JsonException ex)
        {
            services.Logger.LogError(ex, "Failed to parse Auth0 webhook JSON");
            return Results.BadRequest("Invalid JSON payload");
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error processing Auth0 webhook");
            return Results.Problem("Error processing webhook");
        }
    }

    /// <summary>
    /// Extracts user data from Auth0 webhook payload
    /// Auth0 Event Streams format: { "type": "event.type", "data": { "object": { ... } } }
    /// </summary>
    private static Auth0UserData? ExtractUserData(Auth0WebhookServices services, JsonElement root)
    {
        if (!root.TryGetProperty("data", out var dataElement))
        {
            services.Logger.LogWarning("Auth0 webhook missing 'data' field");
            return null;
        }

        if (!dataElement.TryGetProperty("object", out var userElement))
        {
            services.Logger.LogWarning("Auth0 webhook missing 'object' field in data");
            return null;
        }

        // Extract user_id (Auth0 user ID)
        if (!userElement.TryGetProperty("user_id", out var userIdElement))
        {
            services.Logger.LogWarning("Auth0 webhook missing 'user_id' field");
            return null;
        }

        var userId = userIdElement.GetString();
        if (string.IsNullOrEmpty(userId))
        {
            services.Logger.LogWarning("Auth0 webhook has empty 'user_id' field");
            return null;
        }

        // Extract email
        if (!userElement.TryGetProperty("email", out var emailElement))
        {
            services.Logger.LogWarning("Auth0 webhook missing 'email' field for user {UserId}", userId);
            return null;
        }

        var email = emailElement.GetString();
        if (string.IsNullOrEmpty(email))
        {
            services.Logger.LogWarning("Auth0 webhook has empty 'email' field for user {UserId}", userId);
            return null;
        }

        // Extract name (fallback to email if not available)
        var fullName = email; // Default to email
        if (userElement.TryGetProperty("name", out var nameElement))
        {
            var name = nameElement.GetString();
            if (!string.IsNullOrEmpty(name))
            {
                fullName = name;
            }
        }
        else if (userElement.TryGetProperty("nickname", out var nicknameElement))
        {
            var nickname = nicknameElement.GetString();
            if (!string.IsNullOrEmpty(nickname))
            {
                fullName = nickname;
            }
        }

        // Extract connection (authentication method)
        string? connection = null;
        if (userElement.TryGetProperty("identities", out var identitiesElement) && identitiesElement.ValueKind == JsonValueKind.Array)
        {
            // Get the first identity's connection
            if (identitiesElement.GetArrayLength() > 0)
            {
                var firstIdentity = identitiesElement[0];
                if (firstIdentity.TryGetProperty("connection", out var connectionElement))
                {
                    connection = connectionElement.GetString();
                }
            }
        }

        // Extract email_verified
        var emailVerified = false;
        if (userElement.TryGetProperty("email_verified", out var emailVerifiedElement))
        {
            emailVerified = emailVerifiedElement.GetBoolean();
        }

        return new Auth0UserData
        {
            UserId = userId,
            Email = email,
            FullName = fullName,
            Connection = connection,
            EmailVerified = emailVerified
        };
    }

    private static async Task HandleUserCreated(Auth0WebhookServices services, JsonElement root)
    {
        var userData = ExtractUserData(services, root);
        if (userData == null)
        {
            return;
        }

        // Check if user already exists (idempotency)
        var existingUser = await services.UserService.GetById(userData.UserId);
        if (existingUser is not null)
        {
            services.Logger.LogInformation("User {UserId} already exists, skipping creation", userData.UserId);
            return;
        }

        // Create basic user record
        var userDto = new BaseUserDto
        {
            Id = userData.UserId,
            Email = userData.Email,
            FullName = userData.FullName,
            IsOnboarded = false,
            Roles = new List<string>(),
            Connection = userData.Connection,
            EmailVerified = userData.EmailVerified
        };

        try
        {
            await services.UserService.Create(userDto);
            services.Logger.LogInformation("Created user {UserId} ({Email}) from Auth0 webhook", userData.UserId, userData.Email);

            // Send welcome email to user using template
            var templateName = "welcome email";
            var templateData = new Dictionary<string, object>
            {
                { "name", userData.FullName }
            };

            await services.EmailService.SendEmailAsync(
                to: userData.Email,
                template: templateName,
                variables: templateData
            );
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Failed to create user {UserId} from Auth0 webhook", userData.UserId);
            throw; // Re-throw to be caught by outer handler
        }
    }

    private static async Task HandleUserUpdated(Auth0WebhookServices services, JsonElement root)
    {
        var userData = ExtractUserData(services, root);
        if (userData == null)
        {
            return;
        }

        // Check if user exists
        var existingUser = await services.UserService.GetById(userData.UserId);
        if (existingUser == null)
        {
            services.Logger.LogWarning("User {UserId} not found for update, creating new user", userData.UserId);
            // If user doesn't exist, create them (similar to user.created)
            await HandleUserCreated(services, root);
            return;
        }

        // Update user record with latest data from Auth0
        var userDto = new BaseUserDto
        {
            Id = userData.UserId,
            Email = userData.Email,
            FullName = userData.FullName,
            IsOnboarded = existingUser.IsOnboarded, // Preserve onboarding status
            Roles = existingUser.Roles, // Preserve existing roles
            StreetAddress = existingUser.StreetAddress,
            AddressLine2 = existingUser.AddressLine2,
            City = existingUser.City,
            StateProvince = existingUser.StateProvince,
            PostalCode = existingUser.PostalCode,
            Country = existingUser.Country,
            Connection = userData.Connection, // Update connection from Auth0
            EmailVerified = userData.EmailVerified // Update email verification status from Auth0
        };

        try
        {
            await services.UserService.Update(userDto);
            services.Logger.LogInformation("Updated user {UserId} ({Email}) from Auth0 webhook", userData.UserId, userData.Email);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Failed to update user {UserId} from Auth0 webhook", userData.UserId);
            throw; // Re-throw to be caught by outer handler
        }
    }

    private static async Task HandleUserDeleted(Auth0WebhookServices services, JsonElement root)
    {
        var userData = ExtractUserData(services, root);
        if (userData == null)
        {
            return;
        }

        // Check if user exists
        var existingUser = await services.UserService.GetById(userData.UserId);
        if (existingUser == null)
        {
            services.Logger.LogInformation("User {UserId} not found for deletion, may have already been deleted", userData.UserId);
            return;
        }

        try
        {
            // Delete all files associated with the user
            try
            {
                var deletedFileCount = await services.Storage.DeleteAllUserFiles(userData.UserId);
                services.Logger.LogInformation("Deleted {FileCount} files for user {UserId}", deletedFileCount, userData.UserId);
            }
            catch (Exception ex)
            {
                services.Logger.LogError(ex, "Error deleting files for user {UserId}, continuing with user deletion", userData.UserId);
                // Continue with user deletion even if file deletion fails
            }

            await services.UserService.DeleteById(userData.UserId);
            services.Logger.LogInformation("Deleted user {UserId} ({Email}) from Auth0 webhook", userData.UserId, userData.Email);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Failed to delete user {UserId} from Auth0 webhook", userData.UserId);
            throw; // Re-throw to be caught by outer handler
        }
    }
}
