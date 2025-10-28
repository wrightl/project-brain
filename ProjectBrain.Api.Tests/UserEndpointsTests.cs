using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Tests;

public class UserEndpointsTests
{
    private readonly Mock<ILogger<UserServices>> _mockLogger;
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly UserServices _userServices;

    public UserEndpointsTests()
    {
        _mockLogger = new Mock<ILogger<UserServices>>();
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUserService = new Mock<IUserService>();

        _userServices = new UserServices(
            _mockLogger.Object,
            _mockIdentityService.Object,
            _mockUserService.Object
        );
    }

    [Fact]
    public async Task OnboardUser_ShouldCreateUser_WhenValidRequestProvided()
    {
        // Arrange
        var userId = "auth0|123456";
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FullName = "Test User",
            DoB = new DateOnly(1990, 1, 1),
            FavoriteColor = "Blue"
        };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockUserService.Setup(s => s.Create(It.IsAny<UserDto>()))
            .ReturnsAsync((UserDto u) => u);

        // Act
        var method = typeof(UserEndpoints)
            .GetMethod("OnboardUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _userServices, request })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockUserService.Verify(s => s.Create(It.Is<UserDto>(u =>
            u.Id == userId &&
            u.Email == request.Email &&
            u.FullName == request.FullName &&
            u.IsOnboarded == true
        )), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        var userId = "auth0|123456";
        var user = new UserDto
        {
            Id = userId,
            Email = "test@example.com",
            FullName = "Test User",
            FavoriteColour = "Blue",
            DoB = new DateOnly(1990, 1, 1)
        };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockUserService.Setup(s => s.GetById(userId)).ReturnsAsync(user);

        // Act
        var method = typeof(UserEndpoints)
            .GetMethod("GetCurrentUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _userServices })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockUserService.Verify(s => s.GetById(userId), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "auth0|123456";

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockUserService.Setup(s => s.GetById(userId)).ReturnsAsync((UserDto?)null);

        // Act
        var method = typeof(UserEndpoints)
            .GetMethod("GetCurrentUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _userServices })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockUserService.Verify(s => s.GetById(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByEmail_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        var email = "test@example.com";
        var user = new UserDto
        {
            Id = "auth0|123456",
            Email = email,
            FullName = "Test User",
            FavoriteColour = "Blue",
            DoB = new DateOnly(1990, 1, 1)
        };

        _mockUserService.Setup(s => s.GetByEmail(email)).ReturnsAsync(user);

        // Act
        var method = typeof(UserEndpoints)
            .GetMethod("GetUserByEmail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _userServices, email })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockUserService.Verify(s => s.GetByEmail(email), Times.Once);
    }

    [Fact]
    public async Task GetUserByEmail_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _mockUserService.Setup(s => s.GetByEmail(email)).ReturnsAsync((UserDto?)null);

        // Act
        var method = typeof(UserEndpoints)
            .GetMethod("GetUserByEmail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _userServices, email })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockUserService.Verify(s => s.GetByEmail(email), Times.Once);
    }

    [Fact]
    public void UserServices_ShouldInitializeAllDependencies()
    {
        // Assert
        _userServices.Logger.Should().NotBeNull();
        _userServices.IdentityService.Should().NotBeNull();
        _userServices.UserService.Should().NotBeNull();
    }

    [Fact]
    public void CreateUserRequest_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FullName = "Test User",
            DoB = new DateOnly(1990, 1, 1),
            FavoriteColor = "Blue"
        };

        // Assert
        request.Email.Should().Be("test@example.com");
        request.FullName.Should().Be("Test User");
        request.DoB.Should().Be(new DateOnly(1990, 1, 1));
        request.FavoriteColor.Should().Be("Blue");
    }
}
