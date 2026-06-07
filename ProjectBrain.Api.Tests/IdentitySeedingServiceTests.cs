using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Database.Constants;

namespace ProjectBrain.Api.Tests;

public class IdentitySeedingServiceTests
{
    [Fact]
    public async Task EnsureAdminUserSeededAsync_CreatesUserAndAssignsAdminRole()
    {
        var auth0 = new Mock<IAuth0UserManagement>();
        auth0.Setup(m => m.GetUserIdByEmail("admin@test.com")).ReturnsAsync((string?)null);
        auth0.Setup(m => m.CreateUser("admin@test.com", "pass", "Admin", "Username-Password-Authentication", true))
            .ReturnsAsync("auth0|admin");
        auth0.Setup(m => m.UpdateUserRoles("auth0|admin", It.Is<List<string>>(r => r.Contains(AppRoles.Admin))))
            .ReturnsAsync(true);

        var service = new IdentitySeedingService(Mock.Of<ILogger<IdentitySeedingService>>(), auth0.Object);

        var userId = await service.EnsureAdminUserSeededAsync(
            "admin@test.com",
            "pass",
            "Admin",
            "Username-Password-Authentication");

        userId.Should().Be("auth0|admin");
        auth0.Verify(m => m.UpdateUserRoles("auth0|admin", It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task EnsureAuth0UserAsync_ReturnsExistingUserId_WhenUserExists()
    {
        var auth0 = new Mock<IAuth0UserManagement>();
        auth0.Setup(m => m.GetUserIdByEmail("user@test.com")).ReturnsAsync("auth0|existing");

        var service = new IdentitySeedingService(Mock.Of<ILogger<IdentitySeedingService>>(), auth0.Object);

        var userId = await service.EnsureAuth0UserAsync(
            "user@test.com",
            "pass",
            "User",
            "Username-Password-Authentication");

        userId.Should().Be("auth0|existing");
        auth0.Verify(m => m.CreateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true), Times.Never);
    }
}
