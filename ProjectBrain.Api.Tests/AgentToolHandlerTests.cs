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

    [Fact]
    public async Task SearchCoachesToolHandler_IncludesConnectionMetadata()
    {
        var coachUserId = "coach-user-1";
        var connectionId = Guid.NewGuid();
        var coachProfile = new CoachProfile
        {
            Id = 42,
            UserId = coachUserId,
            Bio = "Experienced coach",
            User = new User
            {
                Id = coachUserId,
                Email = "coach@example.com",
                FullName = "Sarah Coach"
            }
        };

        var coachProfileService = new Mock<ICoachProfileService>();
        coachProfileService
            .Setup(s => s.Search(null, null, null, null, null))
            .ReturnsAsync(new List<CoachProfile> { coachProfile });

        var connectionService = new Mock<IConnectionService>();
        connectionService
            .Setup(s => s.GetConnectionAsync("user-1", coachUserId))
            .ReturnsAsync(new Connection
            {
                Id = connectionId,
                UserId = "user-1",
                CoachId = coachUserId,
                Status = "accepted",
                RequestedBy = "user"
            });

        var handler = new SearchCoachesToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>(),
            CoachProfileService = coachProfileService.Object,
            ConnectionService = connectionService.Object,
            FeatureFlagService = Mock.Of<IFeatureFlagService>()
        };

        var result = await handler.ExecuteAsync(context, new Dictionary<string, object>());
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        var coach = doc.RootElement.GetProperty("coaches")[0];
        coach.GetProperty("coachProfileId").GetInt32().Should().Be(42);
        coach.GetProperty("connectionStatus").GetString().Should().Be("connected");
        coach.GetProperty("connectionId").GetString().Should().Be(connectionId.ToString());
    }

    [Fact]
    public async Task GetConnectedCoachesToolHandler_UsesCoachUserIdAndIncludesConnectionId()
    {
        var coachUserId = "coach-user-2";
        var connectionId = Guid.NewGuid().ToString();
        var coachProfile = new CoachProfile
        {
            Id = 7,
            UserId = coachUserId,
            User = new User
            {
                Id = coachUserId,
                Email = "coach2@example.com",
                FullName = "Alex Coach"
            }
        };

        var connectionService = new Mock<IConnectionService>();
        connectionService
            .Setup(s => s.GetConnectedCoachIdsAsync("user-1"))
            .ReturnsAsync(new List<ConnectionWithStatus>
            {
                new()
                {
                    Id = connectionId,
                    UserId = "user-1",
                    CoachId = coachUserId,
                    Status = "accepted"
                }
            });

        var coachProfileService = new Mock<ICoachProfileService>();
        coachProfileService
            .Setup(s => s.GetByUserId(coachUserId))
            .ReturnsAsync(coachProfile);

        var handler = new GetConnectedCoachesToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>(),
            ConnectionService = connectionService.Object,
            CoachProfileService = coachProfileService.Object,
            FeatureFlagService = Mock.Of<IFeatureFlagService>()
        };

        var result = await handler.ExecuteAsync(context, new Dictionary<string, object>());
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        var coach = doc.RootElement.GetProperty("coaches")[0];
        coach.GetProperty("coachProfileId").GetInt32().Should().Be(7);
        coach.GetProperty("connectionStatus").GetString().Should().Be("connected");
        coach.GetProperty("connectionId").GetString().Should().Be(connectionId);

        coachProfileService.Verify(s => s.GetByUserId(coachUserId), Times.Once);
    }

    [Fact]
    public async Task AskUserToolHandler_PreservesCoachActionOnOptions()
    {
        var handler = new AskUserToolHandler();
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };

        var connectionId = Guid.NewGuid().ToString();
        var result = await handler.ExecuteAsync(context, new Dictionary<string, object>
        {
            ["options"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["id"] = "view-profile",
                    ["label"] = "View profile",
                    ["action"] = new Dictionary<string, object>
                    {
                        ["type"] = "view_coach_profile",
                        ["coachProfileId"] = "42"
                    }
                },
                new Dictionary<string, object>
                {
                    ["id"] = "message-coach",
                    ["label"] = "Message coach",
                    ["action"] = new Dictionary<string, object>
                    {
                        ["type"] = "message_coach",
                        ["coachProfileId"] = "42",
                        ["connectionId"] = connectionId
                    }
                }
            }
        });

        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var options = doc.RootElement.GetProperty("options");

        options[0].GetProperty("action").GetProperty("type").GetString().Should().Be("view_coach_profile");
        options[1].GetProperty("action").GetProperty("connectionId").GetString().Should().Be(connectionId);
    }
}
