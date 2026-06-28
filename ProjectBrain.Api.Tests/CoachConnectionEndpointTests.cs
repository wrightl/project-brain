using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Shared.Constants;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Tests;

public class CoachConnectionEndpointTests
{
    private readonly Mock<ILogger<CoachServices>> _mockLogger = new();
    private readonly Mock<IIdentityService> _mockIdentityService = new();
    private readonly Mock<ICoachProfileService> _mockCoachProfileService = new();
    private readonly Mock<IUserService> _mockUserService = new();
    private readonly Mock<IConnectionService> _mockConnectionService = new();
    private readonly Mock<IUserActivityService> _mockUserActivityService = new();
    private readonly Mock<IUserProfileService> _mockUserProfileService = new();
    private readonly Mock<IFeatureGateService> _mockFeatureGateService = new();
    private readonly Mock<ISubscriptionService> _mockSubscriptionService = new();
    private readonly Mock<IUsageTrackingService> _mockUsageTrackingService = new();
    private readonly Mock<ICoachMessageService> _mockCoachMessageService = new();
    private readonly Mock<ICoachRatingService> _mockCoachRatingService = new();
    private readonly Mock<IGeocodingService> _mockGeocodingService = new();
    private readonly Mock<IFakeCoachAutoAcceptService> _mockFakeCoachAutoAcceptService = new();
    private readonly Mock<ICoachSpecialismOptionService> _mockCoachSpecialismOptionService = new();
    private readonly IConfiguration _configuration;
    private readonly CoachServices _coachServices;

    public CoachConnectionEndpointTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FakeCoachAutoReply:Enabled"] = "true",
            })
            .Build();

        _mockFeatureGateService
            .Setup(s => s.CheckFeatureAccessAsync(
                It.IsAny<string>(),
                It.IsAny<UserType>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, null));

        _coachServices = new CoachServices(
            _mockLogger.Object,
            _mockIdentityService.Object,
            _mockCoachProfileService.Object,
            _mockUserService.Object,
            _mockConnectionService.Object,
            _mockUserActivityService.Object,
            _mockUserProfileService.Object,
            _mockFeatureGateService.Object,
            _mockSubscriptionService.Object,
            _mockUsageTrackingService.Object,
            _mockCoachMessageService.Object,
            _mockCoachRatingService.Object,
            _mockGeocodingService.Object,
            _mockFakeCoachAutoAcceptService.Object,
            _mockCoachSpecialismOptionService.Object,
            _configuration);
    }

    [Fact]
    public async Task SendConnectionRequest_ShouldResolveCoachByProfileId_AndCreateConnectionWithCoachUserId()
    {
        const string userId = "windowslive|6c1ef1d4158e0a9b";
        const string coachUserId = "auth0|coach123";
        const int coachProfileId = 30;

        var coachProfile = new CoachProfile
        {
            Id = coachProfileId,
            UserId = coachUserId,
            User = new User
            {
                Id = coachUserId,
                Email = "coach@example.com",
                FullName = "Test Coach",
            },
        };

        var createdConnection = new Connection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CoachId = coachUserId,
            Status = "pending",
            RequestedBy = UserType.User.ToString(),
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockIdentityService.Setup(s => s.GetUserAsync()).ReturnsAsync(new BaseUserDto
        {
            Id = userId,
            Email = "user@example.com",
            FullName = "Test User",
            Roles = [AppRoles.User],
        });
        _mockCoachProfileService
            .Setup(s => s.GetByIdWithRelated(coachProfileId))
            .ReturnsAsync(coachProfile);
        _mockConnectionService
            .Setup(s => s.GetConnectionAsync(userId, coachUserId))
            .ReturnsAsync((Connection?)null);
        _mockConnectionService
            .Setup(s => s.CreateConnectionRequestAsync(
                userId,
                coachUserId,
                UserType.User.ToString(),
                null))
            .ReturnsAsync(createdConnection);

        var method = typeof(CoachEndpoints).GetMethod(
            "SendConnectionRequest",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(
            null,
            new object?[] { _coachServices, coachProfileId.ToString(), null })!;
        var result = await task;

        result.Should().NotBeNull();
        _mockCoachProfileService.Verify(
            s => s.GetByIdWithRelated(coachProfileId),
            Times.Once);
        _mockCoachProfileService.Verify(
            s => s.GetByUserId(It.IsAny<string>()),
            Times.Never);
        _mockConnectionService.Verify(
            s => s.CreateConnectionRequestAsync(
                userId,
                coachUserId,
                UserType.User.ToString(),
                null),
            Times.Once);
    }

    [Fact]
    public async Task SendConnectionRequest_ShouldFallbackToUserId_WhenRouteParamIsNotNumeric()
    {
        const string userId = "windowslive|6c1ef1d4158e0a9b";
        const string coachUserId = "auth0|coach123";

        var coachProfile = new CoachProfile
        {
            Id = 5,
            UserId = coachUserId,
            User = new User
            {
                Id = coachUserId,
                Email = "coach@example.com",
                FullName = "Test Coach",
            },
        };

        var createdConnection = new Connection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CoachId = coachUserId,
            Status = "pending",
            RequestedBy = UserType.User.ToString(),
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockIdentityService.Setup(s => s.GetUserAsync()).ReturnsAsync(new BaseUserDto
        {
            Id = userId,
            Email = "user@example.com",
            FullName = "Test User",
            Roles = [AppRoles.User],
        });
        _mockCoachProfileService
            .Setup(s => s.GetByUserId(coachUserId))
            .ReturnsAsync(coachProfile);
        _mockConnectionService
            .Setup(s => s.GetConnectionAsync(userId, coachUserId))
            .ReturnsAsync((Connection?)null);
        _mockConnectionService
            .Setup(s => s.CreateConnectionRequestAsync(
                userId,
                coachUserId,
                UserType.User.ToString(),
                null))
            .ReturnsAsync(createdConnection);

        var method = typeof(CoachEndpoints).GetMethod(
            "SendConnectionRequest",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(
            null,
            new object?[] { _coachServices, coachUserId, null })!;
        var result = await task;

        result.Should().NotBeNull();
        _mockCoachProfileService.Verify(
            s => s.GetByUserId(coachUserId),
            Times.Once);
        _mockConnectionService.Verify(
            s => s.CreateConnectionRequestAsync(
                userId,
                coachUserId,
                UserType.User.ToString(),
                null),
            Times.Once);
    }

    [Fact]
    public async Task SendConnectionRequest_ShouldAutoAcceptAndReturnConnected_ForTestCoach()
    {
        const string userId = "windowslive|6c1ef1d4158e0a9b";
        const string coachUserId = "auth0|coach123";
        const int coachProfileId = 30;
        var connectionId = Guid.NewGuid();

        var coachProfile = new CoachProfile
        {
            Id = coachProfileId,
            UserId = coachUserId,
            User = new User
            {
                Id = coachUserId,
                Email = "sarah.mitchell@projectbrain.test",
                FullName = "Sarah Mitchell",
            },
        };

        var pendingConnection = new Connection
        {
            Id = connectionId,
            UserId = userId,
            CoachId = coachUserId,
            Status = "pending",
            RequestedBy = UserType.User.ToString(),
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var acceptedConnection = new Connection
        {
            Id = connectionId,
            UserId = userId,
            CoachId = coachUserId,
            Status = "accepted",
            RequestedBy = UserType.User.ToString(),
            RequestedAt = pendingConnection.RequestedAt,
            RespondedAt = DateTime.UtcNow,
            CreatedAt = pendingConnection.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockIdentityService.Setup(s => s.GetUserAsync()).ReturnsAsync(new BaseUserDto
        {
            Id = userId,
            Email = "user@example.com",
            FullName = "Test User",
            Roles = [AppRoles.User],
        });
        _mockCoachProfileService
            .Setup(s => s.GetByIdWithRelated(coachProfileId))
            .ReturnsAsync(coachProfile);
        _mockConnectionService
            .Setup(s => s.GetConnectionAsync(userId, coachUserId))
            .ReturnsAsync((Connection?)null);
        _mockConnectionService
            .Setup(s => s.CreateConnectionRequestAsync(
                userId,
                coachUserId,
                UserType.User.ToString(),
                null))
            .ReturnsAsync(pendingConnection);
        _mockFakeCoachAutoAcceptService
            .Setup(s => s.TryAutoAcceptAsync(pendingConnection, coachProfile.User!.Email))
            .ReturnsAsync(acceptedConnection);

        var method = typeof(CoachEndpoints).GetMethod(
            "SendConnectionRequest",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(
            null,
            new object?[] { _coachServices, coachProfileId.ToString(), null })!;
        var result = await task;

        result.Should().NotBeNull();
        _mockFakeCoachAutoAcceptService.Verify(
            s => s.TryAutoAcceptAsync(pendingConnection, coachProfile.User!.Email),
            Times.Once);

        var createdResult = result as IStatusCodeHttpResult;
        createdResult!.StatusCode.Should().Be(StatusCodes.Status201Created);

        if (result is IValueHttpResult valueResult)
        {
            var response = valueResult.Value as ConnectionResponse;
            response.Should().NotBeNull();
            response!.Status.Should().Be("connected");
        }
    }
}
