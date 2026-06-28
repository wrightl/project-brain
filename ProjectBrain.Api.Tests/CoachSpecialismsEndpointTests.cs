using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Tests;

public class CoachSpecialismsEndpointTests
{
    private readonly Mock<ICoachSpecialismOptionService> _mockCoachSpecialismOptionService = new();
    private readonly CoachServices _coachServices;

    public CoachSpecialismsEndpointTests()
    {
        var configuration = new ConfigurationBuilder().Build();

        _mockCoachSpecialismOptionService
            .Setup(s => s.GetActiveNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectBrain.Shared.Constants.CoachSpecialismCatalog.DefaultOptions.ToList());

        _coachServices = new CoachServices(
            new Mock<ILogger<CoachServices>>().Object,
            new Mock<ProjectBrain.Api.Authentication.IIdentityService>().Object,
            new Mock<ICoachProfileService>().Object,
            new Mock<IUserService>().Object,
            new Mock<IConnectionService>().Object,
            new Mock<IUserActivityService>().Object,
            new Mock<IUserProfileService>().Object,
            new Mock<IFeatureGateService>().Object,
            new Mock<ISubscriptionService>().Object,
            new Mock<IUsageTrackingService>().Object,
            new Mock<ICoachMessageService>().Object,
            new Mock<ICoachRatingService>().Object,
            new Mock<IGeocodingService>().Object,
            new Mock<IFakeCoachAutoAcceptService>().Object,
            _mockCoachSpecialismOptionService.Object,
            configuration);
    }

    [Fact]
    public async Task GetCoachSpecialisms_ShouldReturnActiveCatalogNames()
    {
        var method = typeof(CoachEndpoints).GetMethod(
            "GetCoachSpecialisms",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<Microsoft.AspNetCore.Http.IResult>)method!.Invoke(null, [_coachServices])!;
        var result = await task;

        result.Should().NotBeNull();
        _mockCoachSpecialismOptionService.Verify(
            s => s.GetActiveNamesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
