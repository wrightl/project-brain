namespace ProjectBrain.Database.Interfaces;

public interface IIdentitySeedingService
{
    Task<string> EnsureAdminUserSeededAsync(string email, string password, string fullName, string connection);
}