using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectBrain.Api.Background;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace ProjectBrain.Api.Tests;

public class UserContextTickerEnqueueTests
{
    [Fact]
    public async Task TryEnqueueAsync_ShouldNotThrow_WhenManagerThrows()
    {
        var manager = new Mock<ITimeTickerManager<TimeTickerEntity>>();
        manager.Setup(m => m.AddAsync(It.IsAny<TimeTickerEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ticker unavailable"));

        Func<Task> act = async () => await UserContextTickerEnqueue.TryEnqueueAsync(
            ct => UserContextTickerEnqueue.EnqueueGoalsUploadAsync(manager.Object, "auth0|u1", ct),
            NullLogger.Instance,
            UserContextTickerEnqueue.GoalsUpload,
            "auth0|u1",
            CancellationToken.None);

        await act.Should().NotThrowAsync();
        manager.Verify(m => m.AddAsync(It.IsAny<TimeTickerEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
