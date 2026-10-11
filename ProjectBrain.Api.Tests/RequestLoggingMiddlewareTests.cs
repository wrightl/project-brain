using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Api.Middlewares;
using Xunit;

namespace ProjectBrain.Api.Tests;

public class RequestLoggingMiddlewareTests
{
    private static async Task<(bool NextCalled, Mock<ILogger<RequestLoggingMiddleware>> Logger)> InvokeAsync(string path)
    {
        var nextCalled = false;
        var logger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, logger.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.InvokeAsync(context);

        return (nextCalled, logger);
    }

    private static void VerifyLogCalls(Mock<ILogger<RequestLoggingMiddleware>> logger, Times times) =>
        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

    [Fact]
    public async Task InvokeAsync_LogsStartAndCompletion_ForApiRequests()
    {
        var (nextCalled, logger) = await InvokeAsync("/users/me");

        nextCalled.Should().BeTrue();
        VerifyLogCalls(logger, Times.Exactly(2));
    }

    [Theory]
    [InlineData("/alive")]
    [InlineData("/health")]
    public async Task InvokeAsync_DoesNotLog_ProbeRequests(string path)
    {
        var (nextCalled, logger) = await InvokeAsync(path);

        nextCalled.Should().BeTrue();
        VerifyLogCalls(logger, Times.Never());
    }
}
