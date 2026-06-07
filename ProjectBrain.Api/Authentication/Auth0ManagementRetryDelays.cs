using Polly.Retry;

namespace ProjectBrain.Api.Authentication;

internal static class Auth0ManagementRetryDelays
{
    internal const int MaxRetryAfterSeconds = 60;
    internal static readonly TimeSpan MaxExponentialDelay = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan InitialBackoff = TimeSpan.FromSeconds(1);

    internal static ValueTask<TimeSpan?> GetDelay(RetryDelayGeneratorArguments<HttpResponseMessage> args)
    {
        var response = args.Outcome.Result;
        var retryAfter = response?.Headers.RetryAfter;

        if (retryAfter?.Delta is { } delta)
        {
            return ValueTask.FromResult<TimeSpan?>(CapRetryAfter(delta));
        }

        if (retryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
            {
                return ValueTask.FromResult<TimeSpan?>(CapRetryAfter(wait));
            }
        }

        return ValueTask.FromResult<TimeSpan?>(GetExponentialDelay(args.AttemptNumber));
    }

    internal static TimeSpan GetExponentialDelay(int attemptNumber)
    {
        var exponential = TimeSpan.FromSeconds(InitialBackoff.TotalSeconds * Math.Pow(2, attemptNumber));
        return exponential > MaxExponentialDelay ? MaxExponentialDelay : exponential;
    }

    internal static TimeSpan CapRetryAfter(TimeSpan delay) =>
        delay.TotalSeconds > MaxRetryAfterSeconds
            ? TimeSpan.FromSeconds(MaxRetryAfterSeconds)
            : delay;
}
