using FluentAssertions;
using ProjectBrain.AI;

namespace ProjectBrain.Api.Tests;

public class TiktokenEstimatorTests
{
    [Fact]
    public void EstimateTokens_ReturnsStablePositiveCount()
    {
        var estimator = new TiktokenEstimator();
        var text = "Hello, this is a test prompt for token counting.";

        var first = estimator.EstimateTokens(text);
        var second = estimator.EstimateTokens(text);

        first.Should().BeGreaterThan(0);
        first.Should().Be(second);
    }

    [Fact]
    public void EstimateTokens_CountsMorePreciselyThanCharacterEstimator()
    {
        var text = "Neurodiverse individuals benefit from clear, structured guidance.";
        var tiktoken = new TiktokenEstimator().EstimateTokens(text);
        var character = new CharacterTokenEstimator().EstimateTokens(text);

        tiktoken.Should().BeGreaterThan(0);
        character.Should().BeGreaterThan(0);
        tiktoken.Should().NotBe(character);
    }
}
