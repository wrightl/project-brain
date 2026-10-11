using System.Net;
using FluentAssertions;
using Xunit;

namespace ProjectBrain.Api.IntegrationTests;

public class HealthEndpointIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Alive_ReturnsSuccess()
    {
        // Liveness must stay independent of the database so Container Apps probes do not
        // keep it awake. DB exclusion of /health is covered by
        // HealthEndpointMappingTests in ProjectBrain.Api.Tests.
        var response = await _client.GetAsync("/alive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
