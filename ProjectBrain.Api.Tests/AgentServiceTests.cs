using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain;
using ProjectBrain.Domain.AgentTools;
using ProjectBrain.Domain.Dtos;

namespace ProjectBrain.Api.Tests;

public class AgentServiceTests
{
    [Fact]
    public async Task ProcessAgentInteractionAsync_WhenToolsExecuted_StreamsSecondTurnForSummary()
    {
        var orchestrator = new Mock<IAgentOrchestrator>();
        var toolRegistry = new Mock<IAgentToolRegistry>();
        var toolContextFactory = new Mock<IAgentToolContextFactory>();
        var actionTracking = new Mock<IAgentActionTrackingService>();
        var agentOpenAi = new Mock<IAgentOpenAIService>();
        var logger = new Mock<ILogger<AgentService>>();

        var workflow = new AgentWorkflowState
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Status = "active"
        };

        orchestrator
            .Setup(o => o.CreateWorkflowAsync("user-1", It.IsAny<Guid?>(), "agent_interaction", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        actionTracking
            .Setup(a => a.GetRecentActionsAsync("user-1", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AgentAction>());

        var toolContext = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };
        toolContextFactory
            .Setup(f => f.Create("user-1", It.IsAny<Guid?>(), workflow.Id, It.IsAny<string?>(), It.IsAny<UserType>()))
            .Returns(toolContext);

        var session = new AgentSession { CorrelationId = "trace-1" };
        agentOpenAi
            .Setup(a => a.BeginSessionAsync(It.IsAny<AgentSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var streamTurnCalls = 0;
        agentOpenAi
            .Setup(a => a.StreamTurnAsync(session, It.IsAny<List<Dictionary<string, object>>>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                streamTurnCalls++;
                return streamTurnCalls == 1 ? FirstTurnWithToolCall() : SecondTurnWithText();
            });

        toolRegistry
            .Setup(r => r.ExecuteAsync("create_daily_goals", toolContext, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new { success = true });

        toolRegistry
            .Setup(r => r.TryGetHandler("create_daily_goals"))
            .Returns(new TestToolHandler("create_daily_goals"));

        toolRegistry
            .Setup(r => r.GetEnabledDefinitionsAsync(toolContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dictionary<string, object>>());

        var chatRetrieval = new Mock<IChatRetrievalService>();
        chatRetrieval
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ChatMemoryContext>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatRetrievalResult());

        var service = new AgentService(
            orchestrator.Object,
            toolRegistry.Object,
            toolContextFactory.Object,
            actionTracking.Object,
            agentOpenAi.Object,
            chatRetrieval.Object,
            logger.Object);

        var response = await service.ProcessAgentInteractionAsync(
            "user-1",
            "Set my goals for today",
            Guid.NewGuid(),
            null,
            string.Empty,
            "Alex",
            new List<AgentChatMessage>(),
            new ChatMemoryContext());

        streamTurnCalls.Should().Be(2);
        response.Message.Should().Be("I've set your goals for today.");
        response.ExecutedTools.Should().HaveCount(1);
        response.ExecutedTools[0].ToolName.Should().Be("create_daily_goals");

        agentOpenAi.Verify(
            a => a.AppendToolResults(
                session,
                It.IsAny<string?>(),
                It.Is<IReadOnlyList<AgentToolCall>>(tc => tc.Count == 1),
                It.Is<IReadOnlyList<AgentToolResult>>(tr => tr.Count == 1)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAgentInteractionAsync_WhenNoToolCalls_CompletesInSingleTurn()
    {
        var orchestrator = new Mock<IAgentOrchestrator>();
        var toolRegistry = new Mock<IAgentToolRegistry>();
        var toolContextFactory = new Mock<IAgentToolContextFactory>();
        var actionTracking = new Mock<IAgentActionTrackingService>();
        var agentOpenAi = new Mock<IAgentOpenAIService>();
        var logger = new Mock<ILogger<AgentService>>();

        var workflow = new AgentWorkflowState
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Status = "active"
        };

        orchestrator
            .Setup(o => o.CreateWorkflowAsync("user-1", It.IsAny<Guid?>(), "agent_interaction", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        actionTracking
            .Setup(a => a.GetRecentActionsAsync("user-1", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AgentAction>());

        var session = new AgentSession();
        agentOpenAi
            .Setup(a => a.BeginSessionAsync(It.IsAny<AgentSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        agentOpenAi
            .Setup(a => a.StreamTurnAsync(session, It.IsAny<List<Dictionary<string, object>>>(), It.IsAny<CancellationToken>()))
            .Returns(SecondTurnWithText());

        var toolContext = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };
        toolContextFactory
            .Setup(f => f.Create("user-1", It.IsAny<Guid?>(), workflow.Id, It.IsAny<string?>(), It.IsAny<UserType>()))
            .Returns(toolContext);

        toolRegistry
            .Setup(r => r.GetEnabledDefinitionsAsync(toolContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dictionary<string, object>>());

        var chatRetrieval = new Mock<IChatRetrievalService>();
        chatRetrieval
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ChatMemoryContext>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatRetrievalResult());

        var service = new AgentService(
            orchestrator.Object,
            toolRegistry.Object,
            toolContextFactory.Object,
            actionTracking.Object,
            agentOpenAi.Object,
            chatRetrieval.Object,
            logger.Object);

        var response = await service.ProcessAgentInteractionAsync(
            "user-1",
            "Hello",
            null,
            null,
            string.Empty,
            "Alex",
            new List<AgentChatMessage>(),
            new ChatMemoryContext());

        response.Message.Should().Be("I've set your goals for today.");
        response.ExecutedTools.Should().BeEmpty();

        agentOpenAi.Verify(
            a => a.AppendToolResults(It.IsAny<AgentSession>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<AgentToolCall>>(), It.IsAny<IReadOnlyList<AgentToolResult>>()),
            Times.Never);
    }

    [Fact]
    public async Task StreamAgentInteractionAsync_WhenConfirmationRequired_DoesNotExecuteToolAndEmitsPendingAction()
    {
        var orchestrator = new Mock<IAgentOrchestrator>();
        var toolRegistry = new Mock<IAgentToolRegistry>();
        var toolContextFactory = new Mock<IAgentToolContextFactory>();
        var actionTracking = new Mock<IAgentActionTrackingService>();
        var agentOpenAi = new Mock<IAgentOpenAIService>();
        var logger = new Mock<ILogger<AgentService>>();

        var workflow = new AgentWorkflowState
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Status = "active"
        };

        orchestrator
            .Setup(o => o.CreateWorkflowAsync("user-1", It.IsAny<Guid?>(), "agent_interaction", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        actionTracking
            .Setup(a => a.GetRecentActionsAsync("user-1", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AgentAction>());

        var toolContext = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };
        toolContextFactory
            .Setup(f => f.Create("user-1", It.IsAny<Guid?>(), workflow.Id, It.IsAny<string?>(), It.IsAny<UserType>()))
            .Returns(toolContext);

        var session = new AgentSession();
        agentOpenAi
            .Setup(a => a.BeginSessionAsync(It.IsAny<AgentSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var streamTurnCalls = 0;
        agentOpenAi
            .Setup(a => a.StreamTurnAsync(session, It.IsAny<List<Dictionary<string, object>>>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                streamTurnCalls++;
                return streamTurnCalls == 1 ? FirstTurnWithDeleteToolCall() : SecondTurnWithText();
            });

        toolRegistry
            .Setup(r => r.TryGetHandler("delete_knowledge_resource"))
            .Returns(new TestToolHandler("delete_knowledge_resource", requiresConfirmation: true));

        toolRegistry
            .Setup(r => r.GetEnabledDefinitionsAsync(toolContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dictionary<string, object>>());

        var chatRetrieval = new Mock<IChatRetrievalService>();
        chatRetrieval
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ChatMemoryContext>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatRetrievalResult());

        var service = new AgentService(
            orchestrator.Object,
            toolRegistry.Object,
            toolContextFactory.Object,
            actionTracking.Object,
            agentOpenAi.Object,
            chatRetrieval.Object,
            logger.Object);

        var events = new List<AgentStreamEvent>();
        await foreach (var streamEvent in service.StreamAgentInteractionAsync(
            "user-1",
            "Delete my file",
            Guid.NewGuid(),
            null,
            string.Empty,
            "Alex",
            new List<AgentChatMessage>(),
            new ChatMemoryContext()))
        {
            events.Add(streamEvent);
        }

        events.Should().Contain(e => e.Type == "pending_action");
        events.Should().NotContain(e => e.Type == "tools_executed");

        toolRegistry.Verify(
            r => r.ExecuteAsync("delete_knowledge_resource", It.IsAny<AgentToolContext>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.Never);

        orchestrator.Verify(o => o.PauseWorkflowAsync(workflow, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmPendingActionAsync_ExecutesStoredTool()
    {
        var orchestrator = new Mock<IAgentOrchestrator>();
        var toolRegistry = new Mock<IAgentToolRegistry>();
        var toolContextFactory = new Mock<IAgentToolContextFactory>();
        var actionTracking = new Mock<IAgentActionTrackingService>();
        var agentOpenAi = new Mock<IAgentOpenAIService>();
        var logger = new Mock<ILogger<AgentService>>();
        var chatRetrieval = new Mock<IChatRetrievalService>();

        var workflowId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var workflow = new AgentWorkflowState
        {
            Id = workflowId,
            UserId = "user-1",
            Status = "paused"
        };
        AgentPendingActionStore.Add(workflow, new AgentPendingAction
        {
            Id = actionId,
            ToolName = "delete_knowledge_resource",
            Parameters = new Dictionary<string, object> { ["resourceId"] = resourceId.ToString() },
            Preview = "Delete knowledge resource"
        });

        orchestrator
            .Setup(o => o.LoadWorkflowAsync(workflowId, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        var toolContext = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };
        toolContextFactory
            .Setup(f => f.Create("user-1", It.IsAny<Guid?>(), workflowId, It.IsAny<string?>(), It.IsAny<UserType>()))
            .Returns(toolContext);

        toolRegistry
            .Setup(r => r.ExecuteAsync("delete_knowledge_resource", toolContext, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new { success = true, message = "Resource deleted" });

        var service = new AgentService(
            orchestrator.Object,
            toolRegistry.Object,
            toolContextFactory.Object,
            actionTracking.Object,
            agentOpenAi.Object,
            chatRetrieval.Object,
            logger.Object);

        var result = await service.ConfirmPendingActionAsync("user-1", workflowId, actionId);

        result.Success.Should().BeTrue();
        result.ToolExecution.Should().NotBeNull();
        result.ToolExecution!.ToolName.Should().Be("delete_knowledge_resource");
        toolRegistry.Verify(
            r => r.ExecuteAsync("delete_knowledge_resource", toolContext, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamAgentInteractionAsync_WhenAskUserToolCalled_EmitsUserChoicesAndSkipsSecondTurn()
    {
        var orchestrator = new Mock<IAgentOrchestrator>();
        var toolRegistry = new Mock<IAgentToolRegistry>();
        var toolContextFactory = new Mock<IAgentToolContextFactory>();
        var actionTracking = new Mock<IAgentActionTrackingService>();
        var agentOpenAi = new Mock<IAgentOpenAIService>();
        var logger = new Mock<ILogger<AgentService>>();

        var workflow = new AgentWorkflowState
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Status = "active"
        };

        orchestrator
            .Setup(o => o.CreateWorkflowAsync("user-1", It.IsAny<Guid?>(), "agent_interaction", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        actionTracking
            .Setup(a => a.GetRecentActionsAsync("user-1", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AgentAction>());

        var toolContext = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };
        toolContextFactory
            .Setup(f => f.Create("user-1", It.IsAny<Guid?>(), workflow.Id, It.IsAny<string?>(), It.IsAny<UserType>()))
            .Returns(toolContext);

        var session = new AgentSession();
        agentOpenAi
            .Setup(a => a.BeginSessionAsync(It.IsAny<AgentSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var streamTurnCalls = 0;
        agentOpenAi
            .Setup(a => a.StreamTurnAsync(session, It.IsAny<List<Dictionary<string, object>>>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                streamTurnCalls++;
                return streamTurnCalls == 1 ? FirstTurnWithAskUserToolCall() : SecondTurnWithText();
            });

        toolRegistry
            .Setup(r => r.TryGetHandler("ask_user"))
            .Returns(new TestToolHandler("ask_user", pausesTurn: true));

        toolRegistry
            .Setup(r => r.ExecuteAsync("ask_user", toolContext, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new
            {
                success = true,
                status = "awaiting_user_input",
                prompt = "What would you like to do?",
                allowMultiple = false,
                options = new[]
                {
                    new { id = "goals", label = "Create daily goals" },
                    new { id = "strategies", label = "Suggest coping strategies" }
                }
            });

        toolRegistry
            .Setup(r => r.GetEnabledDefinitionsAsync(toolContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dictionary<string, object>>());

        var chatRetrieval = new Mock<IChatRetrievalService>();
        chatRetrieval
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ChatMemoryContext>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatRetrievalResult());

        var service = new AgentService(
            orchestrator.Object,
            toolRegistry.Object,
            toolContextFactory.Object,
            actionTracking.Object,
            agentOpenAi.Object,
            chatRetrieval.Object,
            logger.Object);

        var events = new List<AgentStreamEvent>();
        await foreach (var streamEvent in service.StreamAgentInteractionAsync(
            "user-1",
            "Help me choose",
            Guid.NewGuid(),
            null,
            string.Empty,
            "Alex",
            new List<AgentChatMessage>(),
            new ChatMemoryContext()))
        {
            events.Add(streamEvent);
        }

        streamTurnCalls.Should().Be(1);
        events.Should().Contain(e => e.Type == "user_choices");
        events.Should().Contain(e => e.Type == "tools_executed");

        agentOpenAi.Verify(
            a => a.AppendToolResults(
                It.IsAny<AgentSession>(),
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<AgentToolCall>>(),
                It.IsAny<IReadOnlyList<AgentToolResult>>()),
            Times.Never);
    }

    private static async IAsyncEnumerable<AgentStreamingUpdate> FirstTurnWithToolCall()
    {
        yield return new AgentStreamingUpdate
        {
            ToolCalls = new List<AgentToolCall>
            {
                new()
                {
                    ToolCallId = "call-1",
                    FunctionName = "create_daily_goals",
                    Parameters = new Dictionary<string, object> { ["goals"] = new[] { "Walk", "Read" } }
                }
            }
        };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<AgentStreamingUpdate> FirstTurnWithDeleteToolCall()
    {
        yield return new AgentStreamingUpdate
        {
            ToolCalls = new List<AgentToolCall>
            {
                new()
                {
                    ToolCallId = "call-2",
                    FunctionName = "delete_knowledge_resource",
                    Parameters = new Dictionary<string, object> { ["resourceId"] = Guid.NewGuid().ToString() }
                }
            }
        };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<AgentStreamingUpdate> FirstTurnWithAskUserToolCall()
    {
        yield return new AgentStreamingUpdate
        {
            Text = "What would you like to do?",
            ToolCalls = new List<AgentToolCall>
            {
                new()
                {
                    ToolCallId = "call-3",
                    FunctionName = "ask_user",
                    Parameters = new Dictionary<string, object>
                    {
                        ["prompt"] = "What would you like to do?",
                        ["options"] = new[]
                        {
                            new Dictionary<string, object> { ["id"] = "goals", ["label"] = "Create daily goals" },
                            new Dictionary<string, object> { ["id"] = "strategies", ["label"] = "Suggest coping strategies" }
                        }
                    }
                }
            }
        };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<AgentStreamingUpdate> SecondTurnWithText()
    {
        yield return new AgentStreamingUpdate { Text = "I've set your goals for today." };
        await Task.CompletedTask;
    }

    private sealed class TestToolHandler : IAgentToolHandler
    {
        public TestToolHandler(string name, bool requiresConfirmation = false, bool pausesTurn = false)
        {
            Name = name;
            RequiresConfirmation = requiresConfirmation;
            PausesTurn = pausesTurn;
        }

        public string Name { get; }
        public bool RequiresConfirmation { get; }
        public bool PausesTurn { get; }

        public Dictionary<string, object> GetDefinition() => new();

        public Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
            => Task.FromResult<object>(new { success = true });
    }
}
