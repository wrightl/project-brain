using ProjectBrain.Api.Authentication;
using ProjectBrain.Database.Interfaces;

public class IdentitySeedingService(ILogger<IdentitySeedingService> logger, IAuth0UserManagement auth0UserManagement) : IIdentitySeedingService
{
    public async Task<string> EnsureAdminUserSeededAsync(string email, string password, string fullName, string connection)
    {
        // Check if admin user already exists in Auth0
        var existingUser = await auth0UserManagement.GetUserIdByEmail(email);
        if (existingUser != null)
        {
            logger.LogInformation("Admin user already exists in Auth0 with ID: {Auth0UserId}", existingUser);
            return existingUser;
        }

        var auth0UserId = await auth0UserManagement.CreateUser(
                        email,
                        password,
                        fullName,
                        connection,
                        true
                    );

        if (string.IsNullOrEmpty(auth0UserId))
        {
            logger.LogError("Failed to create admin user in Auth0. User ID was not returned.");
            throw new InvalidOperationException("Failed to create admin user in Auth0. Check Auth0 configuration and logs.");
        }

        logger.LogInformation("Admin user created in Auth0 with ID: {Auth0UserId}", auth0UserId);

        // Assign admin role in Auth0
        logger.LogInformation("Assigning admin role to user in Auth0...");
        var roleAssigned = await auth0UserManagement.UpdateUserRoles(auth0UserId, new List<string> { "admin" });
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
}