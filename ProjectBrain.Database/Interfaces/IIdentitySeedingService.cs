namespace ProjectBrain.Database.Interfaces;

public interface IIdentitySeedingService
{
    Task<string> EnsureAdminUserSeededAsync(string email, string password, string fullName, string connection);

    Task<string> EnsureAuth0UserAsync(string email, string password, string fullName, string connection);

    Task<bool> AssignAuth0RolesAsync(string auth0UserId, IReadOnlyList<string> roles);
}
