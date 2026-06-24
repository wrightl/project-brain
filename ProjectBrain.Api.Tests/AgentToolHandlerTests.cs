using FluentAssertions;
using Moq;
using System.Text.Json;
using ProjectBrain.Domain;
using ProjectBrain.Domain.AgentTools;

namespace ProjectBrain.Api.Tests;

public class AgentToolHandlerTests
{
    [Fact]
    public async Task GetGoalStreakToolHandler_ReturnsCurrentAndLongestStreak()
    {
        var goalService = new Mock<IGoalService>();
        goalService.Setup(s => s.GetCompletionStreakAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(3);
        goalService.Setup(s => s.GetLongestCompletionStreakAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(7);

        var handler = new GetGoalStreakToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = goalService.Object,
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };

        var result = await handler.ExecuteAsync(context, new Dictionary<string, object>());
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("currentStreak").GetInt32().Should().Be(3);
        doc.RootElement.GetProperty("longestStreak").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task GetIncompleteGoalBacklogToolHandler_ReturnsBacklogItems()
    {
        var goalService = new Mock<IGoalService>();
        goalService
            .Setup(s => s.GetPrioritizedIncompleteGoalBacklogAsync("user-1", 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IncompleteGoalBacklogItem>
            {
                new("Exercise", 2, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            });

        var handler = new GetIncompleteGoalBacklogToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = goalService.Object,
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };

        var result = await handler.ExecuteAsync(context, new Dictionary<string, object>());
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SuggestDailyGoalsToolHandler_ReturnsSuggestions()
    {
        var suggestionService = new Mock<IGoalSuggestionService>();
        suggestionService
            .Setup(s => s.SuggestDailyGoalsAsync("user-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalSuggestionResult
            {
                Goals = new[] { "Walk", "Read" },
                Source = "profile"
            });

        var handler = new SuggestDailyGoalsToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>(),
            GoalSuggestionService = suggestionService.Object,
            UserService = Mock.Of<IUserService>()
        };

        var result = await handler.ExecuteAsync(context, new Dictionary<string, object>());
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchCoachesToolHandler_IsDisabled_WhenCoachFeatureFlagOff()
    {
        var featureFlags = new Mock<IFeatureFlagService>();
        featureFlags.Setup(f => f.IsFeatureEnabled(FeatureFlags.EnableCoachSection)).ReturnsAsync(false);

        var handler = new SearchCoachesToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>(),
            FeatureFlagService = featureFlags.Object
        };

        var enabled = await handler.IsEnabledAsync(context);
        enabled.Should().BeFalse();
    }

    [Fact]
    public async Task UploadKnowledgeDocumentToolHandler_IsDisabled_WhenFileUploadGateFails()
    {
        var featureGate = new Mock<IFeatureGateService>();
        featureGate
            .Setup(f => f.CheckFeatureAccessAsync("user-1", UserType.User, "file_upload"))
            .ReturnsAsync((false, "limit reached"));

        var handler = new UploadKnowledgeDocumentToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>(),
            FeatureGateService = featureGate.Object
        };

        var enabled = await handler.IsEnabledAsync(context);
        enabled.Should().BeFalse();
    }

    [Fact]
    public void ForgetMemoryToolHandler_RequiresConfirmation()
    {
        var handler = new ForgetMemoryToolHandler();
        handler.RequiresConfirmation.Should().BeTrue();
        handler.BuildConfirmationPreview(new Dictionary<string, object>
        {
            ["memoryType"] = "fact",
            ["memoryId"] = Guid.NewGuid().ToString()
        }).Should().Contain("fact");
    }

    [Fact]
    public async Task CreateJournalEntryToolHandler_CreatesEntryViaService()
    {
        var entryId = Guid.NewGuid();
        var journalAgentService = new Mock<IJournalAgentService>();
        journalAgentService
            .Setup(s => s.CreateEntryAsync("user-1", "Today was good", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JournalAgentEntryResult
            {
                Id = entryId,
                Summary = "Good day",
                CreatedAt = DateTime.UtcNow
            });

        var handler = new CreateJournalEntryToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>(),
            JournalAgentService = journalAgentService.Object
        };

        var result = await handler.ExecuteAsync(context, new Dictionary<string, object>
        {
            ["content"] = "Today was good"
        });

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RememberFactToolHandler_UsesMemoryWriteService()
    {
        var factId = Guid.NewGuid();
        var memoryWrite = new Mock<IAgentMemoryWriteService>();
        memoryWrite
            .Setup(s => s.RememberFactAsync("user-1", "I like mornings", "preference", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentRememberFactResult
            {
                Id = factId,
                Content = "I like mornings",
                Category = "preference"
            });

        var handler = new RememberFactToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>(),
            AgentMemoryWriteService = memoryWrite.Object,
            ConversationId = Guid.NewGuid()
        };

        var result = await handler.ExecuteAsync(context, new Dictionary<string, object>
        {
            ["content"] = "I like mornings",
            ["category"] = "preference"
        });

        result.Should().NotBeNull();
    }

    [Fact]
    public void AskUserToolHandler_PausesTurn()
    {
        var handler = new AskUserToolHandler();
        handler.PausesTurn.Should().BeTrue();
    }

    [Fact]
    public async Task AskUserToolHandler_ReturnsStructuredChoices()
    {
        var handler = new AskUserToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };

        var result = await handler.ExecuteAsync(context, new Dictionary<string, object>
        {
            ["prompt"] = "What would you like?",
            ["options"] = new[]
            {
                new Dictionary<string, object> { ["id"] = "goals", ["label"] = "Create daily goals" },
                new Dictionary<string, object> { ["id"] = "strategies", ["label"] = "Suggest coping strategies" }
            }
        });

        result.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(result);
        json.Should().Contain("awaiting_user_input");
        json.Should().Contain("Create daily goals");
    }

    [Fact]
    public async Task AskUserToolHandler_RejectsTooFewOptions()
    {
        var handler = new AskUserToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };

        var act = () => handler.ExecuteAsync(context, new Dictionary<string, object>
        {
            ["options"] = new[]
            {
                new Dictionary<string, object> { ["id"] = "only", ["label"] = "Only option" }
            }
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
