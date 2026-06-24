using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.AI;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Goals;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace ProjectBrain.Api.Tests;

public class GoalSuggestionsEndpointTests
{
    private readonly Mock<ILogger<GoalServices>> _mockLogger = new();
    private readonly Mock<IGoalService> _mockGoalService = new();
    private readonly Mock<IIdentityService> _mockIdentityService = new();
    private readonly Mock<IGoalMutationSideEffects> _mockGoalSideEffects = new();
    private readonly Mock<IGoalsUpdatedBroadcaster> _mockGoalsBroadcaster = new();
    private readonly Mock<IGoalDailySuggestionClient> _mockSuggestionClient = new();
    private readonly Mock<IGoalSuggestionUserContext> _mockUserContext = new();
    private readonly Mock<IUsageTrackingService> _mockUsage = new();

    [Fact]
    public async Task GetSuggestedGoals_ShouldCallAiClient_AndTrackUsage()
    {
        var userId = "auth0|suggest";
        var user = new BaseUserDto { Id = userId, Email = "a@b.com", FullName = "Alex Tester" };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockIdentityService.Setup(s => s.GetUserAsync()).ReturnsAsync(user);
        _mockUserContext.Setup(s => s.LoadOnboardingMarkdownAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Profile");

        var backlog = new List<IncompleteGoalBacklogItem>
        {
            new("Stretch for five minutes", 3, new DateOnly(2025, 1, 10))
        };
        _mockGoalService.Setup(s => s.GetPrioritizedIncompleteGoalBacklogAsync(userId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(backlog);
        _mockGoalService.Setup(s => s.GetTodaysGoalsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProjectBrain.Database.Models.Goal>());

        var aiGoals = new[] { "G1", "G2", "G3" };
        _mockSuggestionClient
            .Setup(c => c.GetSuggestedDailyGoalsAsync(
                userId,
                "Alex",
                "# Profile",
                backlog,
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiGoals);

        var services = new GoalServices(
            _mockLogger.Object,
            _mockGoalService.Object,
            _mockIdentityService.Object,
            _mockGoalSideEffects.Object,
            _mockGoalsBroadcaster.Object,
            _mockSuggestionClient.Object,
            _mockUserContext.Object,
            _mockUsage.Object);

        var method = typeof(GoalEndpoints).GetMethod(
            "GetSuggestedGoals",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { services, CancellationToken.None })!;
        var result = await task;

        result.Should().NotBeNull();
        _mockSuggestionClient.Verify(
            c => c.GetSuggestedDailyGoalsAsync(
                userId,
                "Alex",
                "# Profile",
                backlog,
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUsage.Verify(u => u.TrackAIQueryAsync(userId), Times.Once);
    }
}
