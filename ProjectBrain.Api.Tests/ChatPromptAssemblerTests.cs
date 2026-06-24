using FluentAssertions;
using ProjectBrain.AI;
using ProjectBrain.Domain.Dtos;

namespace ProjectBrain.Api.Tests;

public class ChatPromptAssemblerTests
{
    [Fact]
    public void SelectRecentHistory_WithoutSummary_UsesMaxHistoryMessages()
    {
        var history = Enumerable.Range(1, 12)
            .Select(i => new ProjectBrain.Models.ChatMessage
            {
                Role = i % 2 == 0 ? ProjectBrain.Models.ChatMessageRole.User : ProjectBrain.Models.ChatMessageRole.Assistant,
                Content = $"message-{i}"
            })
            .ToList();

        var memoryContext = new ChatMemoryContext
        {
            RecentMessageWindow = 4,
            MaxHistoryMessages = 10,
            EnableConversationSummary = true,
            ConversationSummary = null
        };

        var selected = ChatPromptAssembler.SelectRecentHistory(history, memoryContext);

        selected.Should().HaveCount(10);
        selected.First().Content.Should().Be("message-3");
        selected.Last().Content.Should().Be("message-12");
    }

    [Fact]
    public void SelectRecentHistory_WithSummary_UsesRecentMessageWindow()
    {
        var history = Enumerable.Range(1, 12)
            .Select(i => new ProjectBrain.Models.ChatMessage
            {
                Role = ProjectBrain.Models.ChatMessageRole.User,
                Content = $"message-{i}"
            })
            .ToList();

        var memoryContext = new ChatMemoryContext
        {
            RecentMessageWindow = 4,
            MaxHistoryMessages = 10,
            EnableConversationSummary = true,
            ConversationSummary = "User discussed anxiety at work."
        };

        var selected = ChatPromptAssembler.SelectRecentHistory(history, memoryContext);

        selected.Should().HaveCount(4);
        selected.First().Content.Should().Be("message-9");
    }

    [Fact]
    public void FormatPreferencesBlock_IncludesPronounTraitsAndParsedValues()
    {
        var prefs = new UserChatPreferences
        {
            PreferredPronoun = "they/them",
            NeurodiverseTraits = new[] { "ADHD", "Autism" },
            ParsedPreferences = new Dictionary<string, string>
            {
                ["timezone"] = "Europe/London"
            }
        };

        var block = ChatPromptAssembler.FormatPreferencesBlock(prefs);

        block.Should().Contain("they/them");
        block.Should().Contain("ADHD");
        block.Should().Contain("timezone: Europe/London");
    }

    [Fact]
    public void BuildSystemPrompt_IncludesPoliciesAndPreferences()
    {
        var memoryContext = new ChatMemoryContext
        {
            Policies =
            [
                new ChatPolicyItem { Key = "AI:Policy:CrisisGuidance", Value = "Encourage urgent help in crisis." }
            ],
            UserPreferences = new UserChatPreferences
            {
                PreferredPronoun = "she/her"
            }
        };

        var prompt = ChatPromptAssembler.BuildSystemPrompt("Alex", hasSources: true, memoryContext);

        prompt.Should().Contain("## Policies");
        prompt.Should().Contain("CrisisGuidance");
        prompt.Should().Contain("## User preferences");
        prompt.Should().Contain("she/her");
        prompt.Should().Contain("Alex");
    }

    [Fact]
    public void BuildUserPrompt_IncludesConversationSummaryWhenPresent()
    {
        var memoryContext = new ChatMemoryContext
        {
            EnableConversationSummary = true,
            ConversationSummary = "Discussed breathing exercises."
        };

        var prompt = ChatPromptAssembler.BuildUserPrompt(
            "What next?",
            "{}",
            string.Empty,
            citationCount: 0,
            memoryContext);

        prompt.Should().Contain("## Conversation so far");
        prompt.Should().Contain("breathing exercises");
        prompt.Should().Contain("What next?");
    }
}
