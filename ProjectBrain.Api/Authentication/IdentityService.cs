using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ProjectBrain.Api.Authentication
{
    public class IdentityService : IIdentityService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService;
        private User? _cachedUser;

        public IdentityService(IHttpContextAccessor httpContextAccessor, IUserService userService)
        {
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.GetUserId();
        public string? UserEmail => _httpContextAccessor.HttpContext?.User?.GetUserEmail();
        // public string? UserName => _httpContextAccessor.HttpContext?.User?.GetUserName();
        // public string? FirstName => UserName?.Split(' ').FirstOrDefault();
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.IsAuthenticated() ?? false;

        public async Task<User?> GetUserAsync()
        {
            // Return cached user if already loaded this request
            if (_cachedUser != null)
                return _cachedUser;

            if (string.IsNullOrEmpty(UserId))
                return null;

            // Load from database and cache for this request
            _cachedUser = await _userService.GetById(UserId);
            return _cachedUser;
        }
    }
}
