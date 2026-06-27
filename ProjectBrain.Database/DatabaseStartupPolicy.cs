using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ProjectBrain.Database;

public static class DatabaseStartupPolicy
{
    public static bool ShouldRunMigrationsOnStartup(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return true;
        }

        var deployEnv = configuration["deploy-env"] ?? configuration["DEPLOY_ENV"];
        return !string.Equals(deployEnv, "staging", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(deployEnv, "production", StringComparison.OrdinalIgnoreCase);
    }
}
