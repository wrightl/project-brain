namespace ProjectBrain.Shared.Constants;

public static class TestUsers
{
    public const string EmailDomain = "projectbrain.test";

    public static bool IsTestCoachEmail(string? email) =>
        email?.EndsWith($"@{EmailDomain}", StringComparison.OrdinalIgnoreCase) == true;
}
