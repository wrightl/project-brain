using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Database;
using ProjectBrain.Database.Constants;
using ProjectBrain.Database.Interfaces;
using ProjectBrain.Database.Models;

namespace ProjectBrain.Database.Tests;

public class TestUsersSeedingTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProjectBrainDbInitializer _initializer;

    public TestUsersSeedingTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockContextLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockContextLogger.Object);
        SeedRoles();

        var serviceProvider = new Mock<IServiceProvider>();
        var initializerLogger = new Mock<ILogger<ProjectBrainDbInitializer>>();
        var startupState = new Mock<IDatabaseStartupState>();
        startupState.Setup(s => s.WaitUntilReadyAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _initializer = new ProjectBrainDbInitializer(
            serviceProvider.Object,
            startupState.Object,
            initializerLogger.Object);
    }

    [Theory]
    [InlineData(false, false, null, false, false)]
    [InlineData(true, false, "Production", false, false)]
    [InlineData(true, true, null, true, true)]
    [InlineData(true, false, "staging", false, true)]
    [InlineData(true, false, "production", null, false)]
    public void ShouldSeedTestUsers_ReturnsExpected(
        bool hasPassword,
        bool enabled,
        string? deployEnv,
        bool isDevelopment,
        bool expected)
    {
        var configData = new Dictionary<string, string?>();
        if (hasPassword)
            configData["TestUsers:Password"] = "secret";
        if (enabled)
            configData["TestUsers:Enabled"] = "true";
        if (deployEnv != null)
            configData["deploy-env"] = deployEnv;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        IHostEnvironment environment = isDevelopment
            ? new TestHostEnvironment("Development")
            : new TestHostEnvironment("Production");

        ProjectBrainDbInitializer.ShouldSeedTestUsers(configuration, environment).Should().Be(expected);
    }

    [Fact]
    public async Task SeedTestUsersAsync_CreatesUserWithExpectedState()
    {
        var identity = new Mock<IIdentitySeedingService>();
        identity.Setup(s => s.EnsureAuth0UserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string email, string _, string _, string _) => $"auth0|{email.Replace("@", "_")}");
        identity.Setup(s => s.AssignAuth0RolesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(true);

        var configuration = BuildConfiguration(enabled: true, password: "TestPass123!");
        var hostEnvironment = new TestHostEnvironment("Development");

        await _initializer.SeedTestUsersAsync(
            _context,
            identity.Object,
            configuration,
            hostEnvironment);

        var testUser = await _context.Users.Include(u => u.UserRoles)
            .FirstAsync(u => u.Email == "testuser1@projectbrain.test");
        testUser.FullName.Should().Be("TestUser1");
        testUser.IsOnboarded.Should().BeFalse();
        testUser.UserRoles.Should().ContainSingle(r => r.RoleName == AppRoles.User);

        var coach = await _context.Users.Include(u => u.UserRoles)
            .FirstAsync(u => u.Email == "sarah.mitchell@projectbrain.test");
        coach.IsOnboarded.Should().BeTrue();
        coach.UserRoles.Should().ContainSingle(r => r.RoleName == AppRoles.Coach);
        coach.City.Should().Be("London");
        coach.PostalCode.Should().Be("SW1A 1AA");
        coach.Latitude.Should().Be(51.5014);
        coach.Longitude.Should().Be(-0.1419);

        var coaches = await _context.Users
            .Where(u => u.Email.EndsWith("@projectbrain.test") && u.UserRoles.Any(r => r.RoleName == AppRoles.Coach))
            .ToListAsync();
        coaches.Should().HaveCount(10);
        coaches.Should().OnlyContain(c =>
            c.PostalCode != null &&
            c.Latitude != null &&
            c.Longitude != null);

        (await _context.CoachProfiles.CountAsync()).Should().Be(10);
    }

    [Fact]
    public async Task SeedTestUsersAsync_IsIdempotentWhenUsersExist()
    {
        var identity = new Mock<IIdentitySeedingService>();
        identity.Setup(s => s.EnsureAuth0UserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string email, string _, string _, string _) => $"auth0|{email.Replace("@", "_")}");
        identity.Setup(s => s.AssignAuth0RolesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(true);

        await _context.Users.AddAsync(new User
        {
            Id = "auth0|existing",
            Email = "testuser1@projectbrain.test",
            FullName = "TestUser1",
            IsOnboarded = false,
            EmailVerified = true,
        });
        await _context.SaveChangesAsync();

        var configuration = BuildConfiguration(enabled: true, password: "TestPass123!");
        var hostEnvironment = new TestHostEnvironment("Development");

        await _initializer.SeedTestUsersAsync(
            _context,
            identity.Object,
            configuration,
            hostEnvironment);

        identity.Verify(
            s => s.EnsureAuth0UserAsync("testuser1@projectbrain.test", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        (await _context.Users.CountAsync(u => u.Email == "testuser1@projectbrain.test")).Should().Be(1);
        (await _context.Users.CountAsync()).Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task SeedTestUsersAsync_SkipsWhenPasswordMissing()
    {
        var identity = new Mock<IIdentitySeedingService>();
        var configuration = BuildConfiguration(enabled: true, password: null);
        var hostEnvironment = new TestHostEnvironment("Development");

        await _initializer.SeedTestUsersAsync(
            _context,
            identity.Object,
            configuration,
            hostEnvironment);

        (await _context.Users.CountAsync()).Should().Be(0);
        identity.VerifyNoOtherCalls();
    }

    private static IConfiguration BuildConfiguration(bool enabled, string? password)
    {
        var data = new Dictionary<string, string?>
        {
            ["TestUsers:Enabled"] = enabled.ToString(),
        };
        if (password != null)
            data["TestUsers:Password"] = password;

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private void SeedRoles()
    {
        _context.Roles.AddRange(
            new Role { Name = AppRoles.User, Description = "User", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Role { Name = AppRoles.Coach, Description = "Coach", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Role { Name = AppRoles.Admin, Description = "Admin", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
