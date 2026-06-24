using FluentAssertions;

namespace ProjectBrain.Api.Tests;

public class ConversationTitleHelperTests
{
    [Fact]
    public void BuildPlaceholderTitle_EmptyContent_ReturnsNewChat()
    {
        ConversationTitleHelper.BuildPlaceholderTitle("").Should().Be("New chat");
        ConversationTitleHelper.BuildPlaceholderTitle("   ").Should().Be("New chat");
    }

    [Fact]
    public void BuildPlaceholderTitle_ShortMessage_ReturnsUnchanged()
    {
        const string message = "Help me plan my afternoon";
        ConversationTitleHelper.BuildPlaceholderTitle(message).Should().Be(message);
    }

    [Fact]
    public void BuildPlaceholderTitle_LongMessage_TruncatesTo128CharsWithEllipsis()
    {
        var longMessage = new string('a', 200);
        var result = ConversationTitleHelper.BuildPlaceholderTitle(longMessage);

        result.Should().HaveLength(128);
        result.Should().EndWith("...");
        result.Should().Be(new string('a', 125) + "...");
    }
}
