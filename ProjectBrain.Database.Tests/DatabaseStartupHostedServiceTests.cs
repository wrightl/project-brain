using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ProjectBrain.Database.Tests;

public class DatabaseStartupHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenConnectEventuallySucceeds_MarksReady()
    {
        var startupState = new DatabaseStartupState();
        var inner = BuildInMemoryProvider();
        var scopeFactory = new FlakyScopeFactory(inner.GetRequiredService<IServiceScopeFactory>(), failCount: 2);
        var provider = new ScopeFactoryServiceProvider(scopeFactory);

        var sut = new DatabaseStartupHostedService(
            provider,
            startupState,
            NullLogger<DatabaseStartupHostedService>.Instance)
        {
            InitialRetryDelay = TimeSpan.FromMilliseconds(5),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5)
        };

        await sut.StartAsync(CancellationToken.None);

        try
        {
            await WaitUntilAsync(() => startupState.IsWarmedUp, TimeSpan.FromSeconds(2));
        }
        finally
        {
            await sut.StopAsync(CancellationToken.None);
        }

        startupState.IsWarmedUp.Should().BeTrue();
        scopeFactory.CreateScopeCalls.Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConnectKeepsFailing_DoesNotGiveUpUntilCancelled()
    {
        var startupState = new DatabaseStartupState();
        var scopeFactory = new AlwaysFailingScopeFactory();
        var provider = new ScopeFactoryServiceProvider(scopeFactory);

        var sut = new DatabaseStartupHostedService(
            provider,
            startupState,
            NullLogger<DatabaseStartupHostedService>.Instance)
        {
            InitialRetryDelay = TimeSpan.FromMilliseconds(5),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5)
        };

        await sut.StartAsync(CancellationToken.None);

        try
        {
            await WaitUntilAsync(() => scopeFactory.CreateScopeCalls > 3, TimeSpan.FromSeconds(2));
            startupState.IsWarmedUp.Should().BeFalse();
        }
        finally
        {
            await sut.StopAsync(CancellationToken.None);
        }

        startupState.IsWarmedUp.Should().BeFalse();
        scopeFactory.CreateScopeCalls.Should().BeGreaterThan(3);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        condition().Should().BeTrue("timed out waiting for condition");
    }

    private static ServiceProvider BuildInMemoryProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<ILogger<AppDbContext>>());
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Root ServiceProvider implements IServiceScopeFactory itself, so a registered
    /// factory is ignored. This wrapper makes CreateScope() use the test factory.
    /// </summary>
    private sealed class ScopeFactoryServiceProvider(IServiceScopeFactory scopeFactory) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory))
            {
                return scopeFactory;
            }

            return null;
        }
    }

    private sealed class FlakyScopeFactory(IServiceScopeFactory inner, int failCount) : IServiceScopeFactory
    {
        public int CreateScopeCalls { get; private set; }

        public IServiceScope CreateScope()
        {
            CreateScopeCalls++;
            if (CreateScopeCalls <= failCount)
            {
                throw new InvalidOperationException("SQL error 40613: database is not currently available");
            }

            return inner.CreateScope();
        }
    }

    private sealed class AlwaysFailingScopeFactory : IServiceScopeFactory
    {
        public int CreateScopeCalls { get; private set; }

        public IServiceScope CreateScope()
        {
            CreateScopeCalls++;
            throw new InvalidOperationException("SQL error 40613: database is not currently available");
        }
    }
}
