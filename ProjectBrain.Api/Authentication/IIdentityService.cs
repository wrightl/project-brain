using System.Security.Claims;

namespace ProjectBrain.Api.Authentication
{
    public interface IIdentityService
    {
        string? UserId { get; }
        string? UserEmail { get; }
        // string? UserName { get; }
        // string? FirstName { get; }
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the full User object from the database based on the authenticated user's identity.
        /// Returns cached user if already loaded during this request.
        /// </summary>
        Task<User?> GetUserAsync();
    }
}
