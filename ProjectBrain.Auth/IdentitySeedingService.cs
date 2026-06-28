using ProjectBrain.Shared.Constants;
using ProjectBrain.Database.Interfaces;

namespace ProjectBrain.Auth;

public class IdentitySeedingService(
    ILogger<IdentitySeedingService> logger,
    IUserManagement userManagement) : IIdentitySeedingService
{
    public async Task<string> EnsureAdminUserSeededAsync(string email, string password, string fullName, string connection)
    {
        var providerUserId = await EnsureProviderUserAsync(email, password, fullName, connection);

        logger.LogInformation("Assigning admin role to user in identity provider...");
        var roleAssigned = await AssignUserRolesAsync(providerUserId, [AppRoles.Admin]);
        if (!roleAssigned)
        {
            logger.LogWarning("Failed to assign admin role in identity provider, but continuing with database seeding.");
        }
        else
        {
            logger.LogInformation("Admin role assigned successfully in identity provider.");
        }

        return providerUserId;
    }

    public Task<string> EnsureProviderUserAsync(string email, string password, string fullName, string connection) =>
        ensureProviderUserAsync(email, password, fullName, connection);

    public Task<bool> AssignUserRolesAsync(string providerUserId, IReadOnlyList<string> roles) =>
        userManagement.UpdateUserRoles(providerUserId, roles.ToList());

    private async Task<string> ensureProviderUserAsync(string email, string password, string fullName, string connection)
    {
        var existingUser = await userManagement.GetUserIdByEmail(email);
        if (existingUser != null)
        {
            logger.LogInformation("User already exists in identity provider with ID: {ProviderUserId}", existingUser);
            return existingUser;
        }

        var providerUserId = await userManagement.CreateUser(
            email,
            password,
            fullName,
            connection,
            emailVerified: true);

        if (string.IsNullOrEmpty(providerUserId))
        {
            logger.LogError("Failed to create user in identity provider for {Email}. User ID was not returned.", email);
            throw new InvalidOperationException($"Failed to create user in identity provider for {email}. Check identity provider configuration and logs.");
        }

        logger.LogInformation("User created in identity provider with ID: {ProviderUserId}", providerUserId);
        return providerUserId;
    }
}
