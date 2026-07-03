using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Auth;
using ProjectBrain.Shared.Constants;

namespace ProjectBrain.Api.Tests;

public class IdentitySeedingServiceTests
{
    [Fact]
    public async Task EnsureAdminUserSeededAsync_CreatesUserAndAssignsAdminRole()
    {
        var userManagement = new Mock<IUserManagement>();
        userManagement.Setup(m => m.GetUserIdByEmail("admin@test.com")).ReturnsAsync((string?)null);
        userManagement.Setup(m => m.CreateUser("admin@test.com", "pass", "Admin", "Username-Password-Authentication", true))
            .ReturnsAsync("auth0|admin");
        userManagement.Setup(m => m.UpdateUserRoles("auth0|admin", It.Is<List<string>>(r => r.Contains(AppRoles.Admin))))
            .ReturnsAsync(true);

        var service = new IdentitySeedingService(Mock.Of<ILogger<IdentitySeedingService>>(), userManagement.Object);

        var userId = await service.EnsureAdminUserSeededAsync(
            "admin@test.com",
            "pass",
            "Admin",
            "Username-Password-Authentication");

        userId.Should().Be("auth0|admin");
        userManagement.Verify(m => m.UpdateUserRoles("auth0|admin", It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task EnsureAdminUserSeededAsync_Throws_WhenAdminRoleAssignmentFails()
    {
        var userManagement = new Mock<IUserManagement>();
        userManagement.Setup(m => m.GetUserIdByEmail("admin@test.com")).ReturnsAsync((string?)null);
        userManagement.Setup(m => m.CreateUser("admin@test.com", "pass", "Admin", "Username-Password-Authentication", true))
            .ReturnsAsync("auth0|admin");
        userManagement.Setup(m => m.UpdateUserRoles("auth0|admin", It.Is<List<string>>(r => r.Contains(AppRoles.Admin))))
            .ReturnsAsync(false);

        var service = new IdentitySeedingService(Mock.Of<ILogger<IdentitySeedingService>>(), userManagement.Object);

        var act = () => service.EnsureAdminUserSeededAsync(
            "admin@test.com",
            "pass",
            "Admin",
            "Username-Password-Authentication");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*admin role*");
    }

    [Fact]
    public async Task EnsureProviderUserAsync_ReturnsExistingUserId_WhenUserExists()
    {
        var userManagement = new Mock<IUserManagement>();
        userManagement.Setup(m => m.GetUserIdByEmail("user@test.com")).ReturnsAsync("auth0|existing");

        var service = new IdentitySeedingService(Mock.Of<ILogger<IdentitySeedingService>>(), userManagement.Object);

        var userId = await service.EnsureProviderUserAsync(
            "user@test.com",
            "pass",
            "User",
            "Username-Password-Authentication");

        userId.Should().Be("auth0|existing");
        userManagement.Verify(m => m.CreateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true), Times.Never);
    }
}
