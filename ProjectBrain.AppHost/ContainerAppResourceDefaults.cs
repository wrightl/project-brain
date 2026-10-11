using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;

namespace ProjectBrain.AppHost;

internal static class ContainerAppResourceDefaults
{
    /// <summary>
    /// Sets 0.25 vCPU and 0.5 Gi on every container in the app template.
    /// CPU uses ParseJson to avoid locale-sensitive Bicep (e.g. 0,25). See dotnet/aspire#8000.
    /// </summary>
    internal static void ApplyQuarterCoreHalfGi(ContainerApp app)
    {
        foreach (var entry in app.Template.Containers)
        {
            var container = entry.Value;
            if (container is null)
            {
                continue;
            }

            container.Resources.Cpu = BicepFunction.ParseJson("0.25").Compile();
            container.Resources.Memory = "0.5Gi";
        }
    }

    /// <summary>
    /// Keeps exactly one replica running: never scaled to zero (no cold start), never scaled out,
    /// because the in-memory cache and SignalR hub assume a single instance.
    /// </summary>
    internal static void SetSingleReplica(ContainerApp app)
    {
        app.Template.Scale.MinReplicas = 1;
        app.Template.Scale.MaxReplicas = 1;
    }
}
