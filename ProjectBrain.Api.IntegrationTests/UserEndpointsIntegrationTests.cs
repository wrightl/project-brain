using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProjectBrain.Api.IntegrationTests;

public class UserEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var anonymousClient = _factory.CreateClient();

        // Act
        var response = await anonymousClient.GetAsync("/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OnboardUser_ShouldCreateUserSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Clear any existing data
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        var request = new
        {
            Email = "newuser@example.com",
            FullName = "New User",
            DoB = "1990-01-01",
            FavoriteColour = "Blue"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/users/me/onboarding", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("newuser@example.com");

        // Verify in database
        var user = context.Users.FirstOrDefault(u => u.Email == "newuser@example.com");
        user.Should().NotBeNull();
        user!.FullName.Should().Be("New User");
        user.IsOnboarded.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUser_WhenAuthenticated()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Id = "test-user-123",
            Email = "test@example.com",
            FullName = "Test User",
            IsOnboarded = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("test@example.com");
        content.Should().Contain("Test User");
    }

    [Fact]
    public async Task GetUserByEmail_ShouldReturnUser_WhenExists()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Id = "user-456",
            Email = "findme@example.com",
            FullName = "Find Me User",
            IsOnboarded = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/users/findme@example.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>
/// Test authentication handler for integration tests
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
            new Claim("sub", "test-user-123"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
