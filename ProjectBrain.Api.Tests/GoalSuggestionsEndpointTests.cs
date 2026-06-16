using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.AI;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Goals;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Shared.Dtos.Goals;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace ProjectBrain.Api.Tests;

public class GoalSuggestionsEndpointTests : IDisposable
{
    private readonly Mock<ILogger<GoalServices>> _mockLogger = new();
    private readonly Mock<IGoalService> _mockGoalService = new();
    private readonly Mock<IIdentityService> _mockIdentityService = new();
    private readonly Mock<IGoalsUpdatedBroadcaster> _mockGoalsBroadcaster = new();
    private readonly Mock<IPushNotificationService> _mockPush = new();
    private readonly Mock<ITimeTickerManager<TimeTickerEntity>> _mockTicker = new();
    private readonly Mock<IGoalDailySuggestionClient> _mockSuggestionClient = new();
    private readonly Mock<IGoalSuggestionUserContext> _mockUserContext = new();
    private readonly Mock<IUsageTrackingService> _mockUsage = new();
    private readonly ServiceProvider _serviceProvider;

    public GoalSuggestionsEndpointTests()
    {
        _serviceProvider = new ServiceCollection()
            .AddSingleton(Mock.Of<IPushNotificationService>())
            .AddSingleton(_mockLogger.Object)
            .BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

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
            _mockGoalsBroadcaster.Object,
            _mockPush.Object,
            _mockTicker.Object,
            _mockSuggestionClient.Object,
            _mockUserContext.Object,
            _mockUsage.Object,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>());

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

    [Fact]
    public async Task CreateOrUpdateGoals_ShouldSendPushFromNewScope()
    {
        var userId = "auth0|goal-update";
        var requestScopedPush = new Mock<IPushNotificationService>();
        var scopedPush = new Mock<IPushNotificationService>();
        var pushCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        scopedPush
            .Setup(p => p.SendDataOnlyToUserAsync(
                userId,
                It.Is<IReadOnlyDictionary<string, string>>(data => data["type"] == "goals_updated"),
                It.IsAny<CancellationToken>()))
            .Callback(() => pushCalled.TrySetResult())
            .Returns(Task.CompletedTask);

        await using var serviceProvider = new ServiceCollection()
            .AddSingleton(scopedPush.Object)
            .AddSingleton(_mockLogger.Object)
            .BuildServiceProvider();

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockGoalService
            .Setup(s => s.CreateOrUpdateGoalsAsync(userId, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Goal { UserId = userId, Date = DateOnly.FromDateTime(DateTime.UtcNow), Index = 0, Message = "First" },
                new Goal { UserId = userId, Date = DateOnly.FromDateTime(DateTime.UtcNow), Index = 1, Message = "Second" },
                new Goal { UserId = userId, Date = DateOnly.FromDateTime(DateTime.UtcNow), Index = 2, Message = "Third" }
            });
        _mockTicker
            .Setup(t => t.AddAsync(It.IsAny<TimeTickerEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new GoalServices(
            _mockLogger.Object,
            _mockGoalService.Object,
            _mockIdentityService.Object,
            _mockGoalsBroadcaster.Object,
            requestScopedPush.Object,
            _mockTicker.Object,
            _mockSuggestionClient.Object,
            _mockUserContext.Object,
            _mockUsage.Object,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        var method = typeof(GoalEndpoints).GetMethod(
            "CreateOrUpdateGoals",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[]
        {
            services,
            new CreateOrUpdateGoalsRequestDto { Goals = ["First", "Second", "Third"] }
        })!;

        var result = await task;

        result.Should().NotBeNull();
        await pushCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        requestScopedPush.Verify(
            p => p.SendDataOnlyToUserAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        scopedPush.Verify(
            p => p.SendDataOnlyToUserAsync(
                userId,
                It.Is<IReadOnlyDictionary<string, string>>(data => data["type"] == "goals_updated"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
