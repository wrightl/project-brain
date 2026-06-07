using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;

namespace ProjectBrain.Api.Tests;

public class DomainToDtoMapperFakeCoachTests
{
    [Fact]
    public async Task SetOnlineStatusAsync_ForcesAvailableForTestCoachWhenEnabled()
    {
        var coachDto = new CoachDto
        {
            Id = "auth0|coach-test",
            Email = "sarah.mitchell@projectbrain.test",
            FullName = "Sarah Mitchell",
            LastActivityAt = null,
        };

        var configuration = BuildConfiguration(enabled: true);
        var userActivityService = new Mock<IUserActivityService>();
        var coachMessageService = new Mock<ICoachMessageService>();

        var before = DateTime.UtcNow;
        await coachDto.SetOnlineStatusAsync(
            userActivityService.Object,
            coachMessageService.Object,
            configuration: configuration);

        coachDto.AvailabilityStatus.Should().Be(AvailabilityStatus.Available);
        coachDto.LastActivityAt.Should().NotBeNull();
        coachDto.LastActivityAt!.Value.Should().BeOnOrAfter(before);
        userActivityService.Verify(
            s => s.IsUserActiveAsync(It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SetOnlineStatusAsync_UsesActivityForRealCoachWhenEnabled()
    {
        var coachDto = new CoachDto
        {
            Id = "auth0|coach-real",
            Email = "real.coach@example.com",
            FullName = "Real Coach",
            LastActivityAt = null,
        };

        var configuration = BuildConfiguration(enabled: true);
        var userActivityService = new Mock<IUserActivityService>();
        userActivityService
            .Setup(s => s.IsUserActiveAsync(coachDto.Id, It.IsAny<int>()))
            .ReturnsAsync(false);
        var coachMessageService = new Mock<ICoachMessageService>();

        await coachDto.SetOnlineStatusAsync(
            userActivityService.Object,
            coachMessageService.Object,
            configuration: configuration);

        coachDto.AvailabilityStatus.Should().Be(AvailabilityStatus.Offline);
        userActivityService.Verify(
            s => s.IsUserActiveAsync(coachDto.Id, It.IsAny<int>()),
            Times.Once);
    }

    private static IConfiguration BuildConfiguration(bool enabled)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FakeCoachAutoReply:Enabled"] = enabled.ToString(),
            })
            .Build();
    }
}
