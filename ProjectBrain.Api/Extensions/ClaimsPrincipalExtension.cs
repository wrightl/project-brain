using System.Security.Claims;

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

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return HasRole(user, "admin");
    }

    public static bool IsCoach(this ClaimsPrincipal user)
    {
        return HasRole(user, "coach");
    }

    // public static bool IsUser(this ClaimsPrincipal user)
    // {
    //     return HasRole(user, "user");
    // }

    private static bool HasRole(this ClaimsPrincipal user, string role)
    {
        var roles = user.FindAll("https://projectbrain.app/roles")
            .Select(c => c.Value)
            .ToList();

        return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}