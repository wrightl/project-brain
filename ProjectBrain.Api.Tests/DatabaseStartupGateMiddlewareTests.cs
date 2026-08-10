using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using ProjectBrain.Api.Middlewares;
using ProjectBrain.Database;
using Xunit;

namespace ProjectBrain.Api.Tests;

public class DatabaseStartupGateMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenWarmedUp_CallsNext()
    {
        var nextCalled = false;
        var middleware = new DatabaseStartupGateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var startupState = new Mock<IDatabaseStartupState>();
        startupState.SetupGet(s => s.IsWarmedUp).Returns(true);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/users";

        await middleware.InvokeAsync(context, startupState.Object);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WhenNotWarmedUp_Returns503WithRetryAfter()
    {
        var nextCalled = false;
        var middleware = new DatabaseStartupGateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var startupState = new Mock<IDatabaseStartupState>();
        startupState.SetupGet(s => s.IsWarmedUp).Returns(false);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/users";

        await middleware.InvokeAsync(context, startupState.Object);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.Headers.RetryAfter.ToString().Should().Be("5");
    }

    [Theory]
    [InlineData("/alive")]
    [InlineData("/health")]
    [InlineData("/health/db")]
    public async Task InvokeAsync_WhenNotWarmedUp_ExemptsProbePaths(string path)
    {
        var nextCalled = false;
        var middleware = new DatabaseStartupGateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var startupState = new Mock<IDatabaseStartupState>();
        startupState.SetupGet(s => s.IsWarmedUp).Returns(false);

        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.InvokeAsync(context, startupState.Object);

        nextCalled.Should().BeTrue();
    }
}
