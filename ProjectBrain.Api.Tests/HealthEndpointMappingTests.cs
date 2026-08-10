using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ProjectBrain.Api.Tests;

/// <summary>
/// Verifies readiness (/health) excludes db-tagged checks while /health/db includes them.
/// </summary>
public class HealthEndpointMappingTests
{
    [Fact]
    public async Task Health_ExcludesDbTaggedChecks_HealthDb_IncludesThem()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHealthChecks()
                        .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
                        .AddCheck("database-migrations", () => HealthCheckResult.Unhealthy("db down"), ["ready", "db"]);
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                        {
                            Predicate = r => !r.Tags.Contains("db")
                        });
                        endpoints.MapHealthChecks("/health/db", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                        {
                            Predicate = r => r.Tags.Contains("db")
                        });
                        endpoints.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                        {
                            Predicate = r => r.Tags.Contains("live")
                        });
                    });
                });
            })
            .StartAsync();

        var client = host.GetTestClient();

        var health = await client.GetAsync("/health");
        var healthDb = await client.GetAsync("/health/db");
        var alive = await client.GetAsync("/alive");

        health.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        healthDb.StatusCode.Should().Be(System.Net.HttpStatusCode.ServiceUnavailable);
        alive.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
