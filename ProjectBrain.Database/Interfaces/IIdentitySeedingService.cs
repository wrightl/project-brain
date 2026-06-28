namespace ProjectBrain.Database.Interfaces;

public interface IIdentitySeedingService
{
    Task<string> EnsureAdminUserSeededAsync(string email, string password, string fullName, string connection);

    Task<string> EnsureProviderUserAsync(string email, string password, string fullName, string connection);

    Task<bool> AssignUserRolesAsync(string providerUserId, IReadOnlyList<string> roles);
}
