namespace ProjectBrain.Domain;

public interface IFeatureGateService
{
    Task<bool> CanUseFeatureAsync(string userId, string userType, string feature);
    Task<(bool Allowed, string? ErrorMessage)> CheckFeatureAccessAsync(string userId, string userType, string feature);
}

