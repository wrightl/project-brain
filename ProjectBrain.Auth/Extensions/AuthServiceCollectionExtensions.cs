using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using ProjectBrain.Auth.Auth0;

namespace ProjectBrain.Auth;

public static class AuthServiceCollectionExtensions
{
    public static WebApplicationBuilder AddAuth(this WebApplicationBuilder builder)
    {
        ((IHostApplicationBuilder)builder).AddAuth();
        return builder;
    }

    public static IHostApplicationBuilder AddAuth(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();

        builder.Services.AddHttpClient(Auth0ManagementHttp.ClientName);

        builder.Services.AddResiliencePipeline<string, HttpResponseMessage>(Auth0ManagementHttp.PipelineName, (pipelineBuilder, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(Auth0ManagementHttp.ClientName);

            pipelineBuilder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 4,
                Delay = Auth0ManagementRetryDelays.InitialBackoff,
                BackoffType = DelayBackoffType.Exponential,
                MaxDelay = Auth0ManagementRetryDelays.MaxExponentialDelay,
                ShouldHandle = static args => args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests
                    ? PredicateResult.True()
                    : PredicateResult.False(),
                DelayGenerator = Auth0ManagementRetryDelays.GetDelay,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Identity provider rate limit (429) for {RequestUri}, retry {Attempt} after {Delay}ms",
                        args.Outcome.Result?.RequestMessage?.RequestUri,
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds);
                    return default;
                },
            });
        });

        builder.Services.AddScoped<IUserManagement, Auth0UserManagement>();
        builder.Services.AddScoped<Auth0UserManagementServices>();

        return builder;
    }
}
