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
}