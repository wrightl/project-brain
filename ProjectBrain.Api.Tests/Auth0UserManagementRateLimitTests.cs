using System.Net;
using FluentAssertions;
using Polly;
using Polly.Retry;
using ProjectBrain.Api.Authentication;

namespace ProjectBrain.Api.Tests;

public class Auth0UserManagementRateLimitTests
{
    [Fact]
    public async Task GetDelay_UsesRetryAfterSeconds()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(5));

        var delay = await Auth0ManagementRetryDelays.GetDelay(CreateDelayArgs(response, attemptNumber: 0));

        delay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetDelay_CapsRetryAfterAt60Seconds()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(120));

        var delay = await Auth0ManagementRetryDelays.GetDelay(CreateDelayArgs(response, attemptNumber: 0));

        delay.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    public async Task GetDelay_UsesExponentialBackoffWhenNoHeader(int attemptNumber, int expectedSeconds)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        var delay = await Auth0ManagementRetryDelays.GetDelay(CreateDelayArgs(response, attemptNumber));

        delay.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public void GetExponentialDelay_CapsAt30Seconds()
    {
        Auth0ManagementRetryDelays.GetExponentialDelay(attemptNumber: 10)
            .Should().Be(TimeSpan.FromSeconds(30));
    }

    private static RetryDelayGeneratorArguments<HttpResponseMessage> CreateDelayArgs(
        HttpResponseMessage response,
        int attemptNumber)
    {
        var context = ResilienceContextPool.Shared.Get();
        return new RetryDelayGeneratorArguments<HttpResponseMessage>(
            context,
            Outcome.FromResult(response),
            attemptNumber);
    }
}
