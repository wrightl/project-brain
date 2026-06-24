using FluentAssertions;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.AgentTools;

namespace ProjectBrain.Api.Tests;

public class AgentToolRegistryTests
{
    [Fact]
    public async Task GetEnabledDefinitionsAsync_ExcludesDisabledHandlers()
    {
        var enabledHandler = new Mock<IAgentToolHandler>();
        enabledHandler.Setup(h => h.Name).Returns("enabled_tool");
        enabledHandler.Setup(h => h.IsEnabledAsync(It.IsAny<AgentToolContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        enabledHandler.Setup(h => h.GetDefinition()).Returns(new Dictionary<string, object> { ["name"] = "enabled_tool" });

        var disabledHandler = new Mock<IAgentToolHandler>();
        disabledHandler.Setup(h => h.Name).Returns("disabled_tool");
        disabledHandler.Setup(h => h.IsEnabledAsync(It.IsAny<AgentToolContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var registry = new AgentToolRegistry(new[] { enabledHandler.Object, disabledHandler.Object });
        var context = new AgentToolContext
        {
            UserId = "user-1",
            GoalService = Mock.Of<IGoalService>(),
            GoalMutationSideEffects = Mock.Of<IGoalMutationSideEffects>()
        };

        var definitions = await registry.GetEnabledDefinitionsAsync(context);

        definitions.Should().HaveCount(1);
        definitions[0]["name"].Should().Be("enabled_tool");
    }
}
