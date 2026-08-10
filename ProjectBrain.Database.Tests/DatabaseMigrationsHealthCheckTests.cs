using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectBrain.Database;
using Xunit;

namespace ProjectBrain.Database.Tests;

public class DatabaseMigrationsHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenNotWarmedUp_ReturnsUnhealthy()
    {
        var startupState = new Mock<IDatabaseStartupState>();
        startupState.SetupGet(s => s.IsWarmedUp).Returns(false);

        var sut = new DatabaseMigrationsHealthCheck(
            Mock.Of<IServiceProvider>(),
            startupState.Object,
            NullLogger<DatabaseMigrationsHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        result.Status.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);
        result.Description.Should().Contain("warmup");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenMigrationsApplied_ReturnsHealthyWithoutDbAccess()
    {
        var startupState = new Mock<IDatabaseStartupState>();
        startupState.SetupGet(s => s.IsWarmedUp).Returns(true);
        startupState.SetupGet(s => s.AreMigrationsApplied).Returns(true);

        var services = new Mock<IServiceProvider>(MockBehavior.Strict);

        var sut = new DatabaseMigrationsHealthCheck(
            services.Object,
            startupState.Object,
            NullLogger<DatabaseMigrationsHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        result.Status.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
        services.VerifyNoOtherCalls();
    }
}
