using FluentAssertions;
using ProjectBrain.AI;
using ProjectBrain.Domain.Dtos;

namespace ProjectBrain.Api.Tests;

public class ChatTurnTraceBuilderTests
{
    [Fact]
    public void Build_IncludesFactAndEpisodeIds()
    {
        var factId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var memoryContext = new ChatMemoryContext
        {
            Facts = [new RetrievedUserFact { Id = factId, Content = "Prefers concise answers" }],
            Episodes =
            [
                new RetrievedUserEpisode
                {
                    Id = episodeId,
                    Summary = "Morning walk helped",
                    Topic = "focus",
                    Outcome = "helpful"
                }
            ],
            MemoryRetrievalMode = "hybrid"
        };

        var envelope = ChatTurnTraceBuilder.Build(
            "corr-1",
            Guid.NewGuid(),
            "auth0|user",
            memoryContext,
            recentHistoryCount: 2,
            citationCount: 0,
            citationIds: Array.Empty<string>(),
            retrievalMode: "agent",
            estimatedTokens: 1200,
            maxTotalTokens: 7000,
            truncatedSources: false,
            slotTraces: [new PromptSlotTrace { SlotName = "facts", EstimatedTokens = 40 }]);

        envelope.Memory.FactIdsRetrieved.Should().Contain(factId.ToString());
        envelope.Memory.EpisodeIdsRetrieved.Should().Contain(episodeId.ToString());
        envelope.Retrieval.RetrievalMode.Should().Be("agent");
        envelope.Prompt.Slots.Should().ContainSingle(s => s.SlotName == "facts");
    }
}
