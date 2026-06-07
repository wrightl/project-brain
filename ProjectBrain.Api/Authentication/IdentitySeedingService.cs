using ProjectBrain.Database.Constants;
using ProjectBrain.Database.Interfaces;

namespace ProjectBrain.Api.Authentication;

public class IdentitySeedingService(
    ILogger<IdentitySeedingService> logger,
    IAuth0UserManagement auth0UserManagement) : IIdentitySeedingService
{
    public async Task<string> EnsureAdminUserSeededAsync(string email, string password, string fullName, string connection)
    {
        var auth0UserId = await EnsureAuth0UserAsync(email, password, fullName, connection);

        logger.LogInformation("Assigning admin role to user in Auth0...");
        var roleAssigned = await AssignAuth0RolesAsync(auth0UserId, [AppRoles.Admin]);
        if (!roleAssigned)
        {
            logger.LogWarning("Failed to assign admin role in Auth0, but continuing with database seeding.");
        }
        else
        {
            logger.LogInformation("Admin role assigned successfully in Auth0.");
        }

        return auth0UserId;
    }

    public Task<string> EnsureAuth0UserAsync(string email, string password, string fullName, string connection) =>
        ensureAuth0UserAsync(email, password, fullName, connection);

    public Task<bool> AssignAuth0RolesAsync(string auth0UserId, IReadOnlyList<string> roles) =>
        auth0UserManagement.UpdateUserRoles(auth0UserId, roles.ToList());

    private async Task<string> ensureAuth0UserAsync(string email, string password, string fullName, string connection)
    {
        var existingUser = await auth0UserManagement.GetUserIdByEmail(email);
        if (existingUser != null)
        {
            logger.LogInformation("User already exists in Auth0 with ID: {Auth0UserId}", existingUser);
            return existingUser;
        }

        var auth0UserId = await auth0UserManagement.CreateUser(
            email,
            password,
            fullName,
            connection,
            emailVerified: true);

        if (string.IsNullOrEmpty(auth0UserId))
        {
            logger.LogError("Failed to create user in Auth0 for {Email}. User ID was not returned.", email);
            throw new InvalidOperationException($"Failed to create user in Auth0 for {email}. Check Auth0 configuration and logs.");
        }

        logger.LogInformation("User created in Auth0 with ID: {Auth0UserId}", auth0UserId);
        return auth0UserId;
    }
}
