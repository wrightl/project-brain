namespace ProjectBrain.Database.Constants;

public static class AppRoles
{
    public const string User = "user";
    public const string Coach = "coach";
    public const string Admin = "admin";

    public static readonly string[] All = [User, Coach, Admin];

    public static bool IsValid(string? role) =>
        !string.IsNullOrEmpty(role) &&
        All.Contains(role, StringComparer.OrdinalIgnoreCase);
}
