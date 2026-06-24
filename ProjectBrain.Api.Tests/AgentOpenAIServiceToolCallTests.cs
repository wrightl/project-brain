using System.Text;
using System.Text.Json;
using FluentAssertions;
using OpenAI.Chat;

namespace ProjectBrain.Api.Tests;

public class AgentOpenAIServiceToolCallTests
{
    [Fact]
    public void StreamingToolCallUpdates_AggregateByIndex_ProducesSingleNamedToolCall()
    {
        var updates = new[]
        {
            OpenAIChatModelFactory.StreamingChatToolCallUpdate(
                index: 0,
                toolCallId: null,
                kind: ChatToolCallKind.Function,
                functionName: null,
                functionArgumentsUpdate: null),
            OpenAIChatModelFactory.StreamingChatToolCallUpdate(
                index: 0,
                toolCallId: "call_abc",
                kind: ChatToolCallKind.Function,
                functionName: "create_daily_goals",
                functionArgumentsUpdate: null),
            OpenAIChatModelFactory.StreamingChatToolCallUpdate(
                index: 0,
                toolCallId: null,
                kind: ChatToolCallKind.Function,
                functionName: null,
                functionArgumentsUpdate: BinaryData.FromString("{\"goals\":[\"Walk\"]")),
        };

        var byIndex = new Dictionary<int, (string Id, string Name, StringBuilder Args)>();

        foreach (var update in updates)
        {
            if (!byIndex.TryGetValue(update.Index, out var acc))
            {
                acc = (string.Empty, string.Empty, new StringBuilder());
            }

            if (!string.IsNullOrEmpty(update.ToolCallId))
            {
                acc.Id = update.ToolCallId;
            }

            if (!string.IsNullOrEmpty(update.FunctionName))
            {
                acc.Name = update.FunctionName;
            }

            if (update.FunctionArgumentsUpdate is { } args)
            {
                acc.Args.Append(args.ToString());
            }

            byIndex[update.Index] = acc;
        }

        byIndex.Should().HaveCount(1);
        var aggregated = byIndex[0];
        aggregated.Id.Should().Be("call_abc");
        aggregated.Name.Should().Be("create_daily_goals");
        aggregated.Args.ToString().Should().Contain("Walk");
    }
}
