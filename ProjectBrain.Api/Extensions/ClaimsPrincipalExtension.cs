using System.Security.Claims;
using ProjectBrain.Database.Constants;

public static class ClaimsPrincipalExtension
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public static string? GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    public static bool IsAuthenticated(this ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated ?? false;
    }

    public static IReadOnlyList<string> GetAppRoles(this ClaimsPrincipal user) =>
        user.FindAll(AuthClaimTypes.Roles).Select(c => c.Value).ToList();

    public static bool HasAppRole(this ClaimsPrincipal user, string role) =>
        user.GetAppRoles().Contains(role, StringComparer.OrdinalIgnoreCase);

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.HasAppRole(AppRoles.Admin);

    public static bool IsCoach(this ClaimsPrincipal user) =>
        user.HasAppRole(AppRoles.Coach);
}
