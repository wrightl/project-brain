using ProjectBrain.Domain;

namespace ProjectBrain.Auth;

public interface IUserManagement
{
    Task<string?> CreateUser(string email, string password, string fullName, string connection, bool emailVerified);
    Task<bool> UpdateUserRoles(string userId, List<string> roles);
    Task<bool> UpdateUser(string userId, BaseUserDto user);
    Task<bool> DeleteUserById(string id);
    Task<string?> GetUserIdByEmail(string email);
}
