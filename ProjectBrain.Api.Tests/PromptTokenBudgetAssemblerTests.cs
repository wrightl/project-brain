using FluentAssertions;
using ProjectBrain.AI;
using ProjectBrain.Domain.Dtos;

namespace ProjectBrain.Api.Tests;

public class PromptTokenBudgetAssemblerTests
{
    private readonly CharacterTokenEstimator _estimator = new();

    [Fact]
    public void Assemble_DropsEpisodesBeforeFacts_WhenOverBudget()
    {
        var memoryContext = new ChatMemoryContext
        {
            Facts =
            [
                new RetrievedUserFact { Id = Guid.NewGuid(), Content = "Likes short answers" }
            ],
            Episodes =
            [
                new RetrievedUserEpisode
                {
                    Id = Guid.NewGuid(),
                    Summary = "Deep breathing helped before presentations and reduced anxiety noticeably",
                    Topic = "anxiety",
                    Outcome = "helped"
                },
                new RetrievedUserEpisode
                {
                    Id = Guid.NewGuid(),
                    Summary = "Walking outside reset focus after long meetings and improved energy",
                    Topic = "focus",
                    Outcome = "helped"
                },
                new RetrievedUserEpisode
                {
                    Id = Guid.NewGuid(),
                    Summary = "Listening to calm music before bed improved sleep quality",
                    Topic = "sleep",
                    Outcome = "helped"
                }
            ]
        };

        var budget = new PromptBudgetSettings
        {
            EnablePromptBudget = true,
            SystemReserve = 200,
            PoliciesReserve = 100,
            PreferencesReserve = 100,
            QueryReserve = 100,
            SummaryReserve = 50,
            FactsReserve = 150,
            EpisodesReserve = 20,
            OnboardingReserve = 50,
            HistoryReserve = 50
        };

        var result = PromptTokenBudgetAssembler.Assemble(
            "How can I prepare for tomorrow?",
            "{}",
            string.Empty,
            citationCount: 0,
            memoryContext,
            Array.Empty<ProjectBrain.Models.ChatMessage>(),
            budget,
            maxTotalTokens: 1200,
            _estimator);

        var factsSlot = result.SlotTraces.First(s => s.SlotName == "facts");
        var episodesSlot = result.SlotTraces.First(s => s.SlotName == "episodes");

        episodesSlot.DroppedCount.Should().BeGreaterThan(0);
        factsSlot.DroppedCount.Should().Be(0);
        result.UserPrompt.Should().Contain("Likes short answers");
        result.UserPrompt.Should().NotContain("Listening to calm music");
    }

    [Fact]
    public void Assemble_PopulatesSlotTraces()
    {
        var memoryContext = new ChatMemoryContext
        {
            ConversationSummary = "User discussed work stress.",
            EnableConversationSummary = true,
            Facts =
            [
                new RetrievedUserFact { Id = Guid.NewGuid(), Content = "Uses noise-cancelling headphones" }
            ]
        };

        var budget = new PromptBudgetSettings
        {
            EnablePromptBudget = true,
            SystemReserve = 200,
            PoliciesReserve = 100,
            PreferencesReserve = 100,
            QueryReserve = 100,
            SummaryReserve = 200,
            FactsReserve = 200,
            EpisodesReserve = 100,
            OnboardingReserve = 100,
            HistoryReserve = 200
        };

        var result = PromptTokenBudgetAssembler.Assemble(
            "What should I try?",
            "{\"name\":\"Alex\"}",
            "source content",
            citationCount: 1,
            memoryContext,
            Array.Empty<ProjectBrain.Models.ChatMessage>(),
            budget,
            maxTotalTokens: 2000,
            _estimator);

        result.SlotTraces.Should().Contain(s => s.SlotName == "summary" && s.EstimatedTokens > 0);
        result.SlotTraces.Should().Contain(s => s.SlotName == "facts");
        result.SlotTraces.Should().Contain(s => s.SlotName == "query");
        result.SlotTraces.Should().Contain(s => s.SlotName == "sources");
    }
}
