using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain;
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
            .Setup(f => f.Create("user-1", It.IsAny<Guid?>(), workflow.Id, It.IsAny<string?>()))
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
            .Setup(r => r.GetAllDefinitions())
            .Returns(new List<Dictionary<string, object>>());

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
            new ChatMemoryContext(),
            CancellationToken.None);

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

        toolContextFactory
            .Setup(f => f.Create("user-1", It.IsAny<Guid?>(), workflow.Id, It.IsAny<string?>()))
            .Returns(new AgentToolContext
            {
                UserId = "user-1",
                GoalService = Mock.Of<IGoalService>(),
                GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
            });

        toolRegistry
            .Setup(r => r.GetAllDefinitions())
            .Returns(new List<Dictionary<string, object>>());

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
            new ChatMemoryContext(),
            CancellationToken.None);

        response.Message.Should().Be("I've set your goals for today.");
        response.ExecutedTools.Should().BeEmpty();

        agentOpenAi.Verify(
            a => a.AppendToolResults(It.IsAny<AgentSession>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<AgentToolCall>>(), It.IsAny<IReadOnlyList<AgentToolResult>>()),
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

    private static async IAsyncEnumerable<AgentStreamingUpdate> SecondTurnWithText()
    {
        yield return new AgentStreamingUpdate { Text = "I've set your goals for today." };
        await Task.CompletedTask;
    }
}
