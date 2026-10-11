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
        var response = await _client.GetAsync("/alive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_IsMapped()
    {
        // The Azure client health checks (Search, Blob) can't reach the fake endpoints used here,
        // so only check that the readiness endpoint exists.
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthDb_IsNotMapped()
    {
        // The database health endpoint was removed so nothing polls the database and it can scale to zero.
        var response = await _client.GetAsync("/health/db");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
