using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.AI;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Background;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.Journal;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace ProjectBrain.Api.Tests;

public class JournalEndpointsTests
{
    [Fact]
    public async Task CreateJournalEntry_ShouldStillCreate_WhenEnqueueFails()
    {
        var userId = "auth0|journal-user";
        var request = new CreateJournalEntryRequestDto { Content = "Today was hard, but I got through it." };

        var mockJournalEntryService = new Mock<IJournalEntryService>();
        var mockJournalEntryRepository = new Mock<IJournalEntryRepository>();
        var mockSystemTagService = new Mock<ISystemTagService>();
        var mockUserProfileService = new Mock<IUserProfileService>();
        var mockJournalStreakService = new Mock<IJournalStreakService>();
        var mockIdentityService = new Mock<IIdentityService>();
        var mockLogger = new Mock<ILogger<JournalServices>>();
        var mockTimeTickerManager = new Mock<ITimeTickerManager<TimeTickerEntity>>();
        var mockSearchIndexService = new Mock<ISearchIndexService>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockApplicationSettingsService = new Mock<IApplicationSettingsService>();
        var mockAzureLogger = new Mock<ILogger<AzureOpenAIServices>>();
        var azureOpenAiServices = new AzureOpenAIServices(
            null!,
            mockSearchIndexService.Object,
            mockConfiguration.Object,
            mockApplicationSettingsService.Object,
            mockAzureLogger.Object);
        var azureOpenAi = new AzureOpenAI(azureOpenAiServices);

        mockIdentityService.Setup(s => s.UserId).Returns(userId);
        mockJournalEntryService
            .Setup(s => s.Add(It.IsAny<JournalEntry>(), It.IsAny<IEnumerable<Guid>?>(), It.IsAny<IEnumerable<SystemTagAssignment>?>()))
            .ReturnsAsync((JournalEntry entry, IEnumerable<Guid>? _, IEnumerable<SystemTagAssignment>? __) => entry);

        mockTimeTickerManager
            .Setup(m => m.AddAsync(It.IsAny<TimeTickerEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ticker unavailable"));

        var services = new JournalServices(
            mockJournalEntryService.Object,
            mockJournalEntryRepository.Object,
            mockSystemTagService.Object,
            mockUserProfileService.Object,
            mockJournalStreakService.Object,
            mockIdentityService.Object,
            mockLogger.Object,
            mockTimeTickerManager.Object,
            azureOpenAi);

        var method = typeof(JournalEndpoints)
            .GetMethod("CreateJournalEntry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var task = (Task<IResult>)method!.Invoke(null, new object[] { services, request })!;
        var result = await task;

        result.Should().NotBeNull();
        result.Should().BeOfType<Created<JournalEntryResponseDto>>();
        mockJournalEntryService.Verify(
            s => s.Add(
                It.Is<JournalEntry>(j => j.UserId == userId && j.Content == request.Content),
                It.IsAny<IEnumerable<Guid>?>(),
                It.IsAny<IEnumerable<SystemTagAssignment>?>()),
            Times.Once);
        mockTimeTickerManager.Verify(
            m => m.AddAsync(It.IsAny<TimeTickerEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
