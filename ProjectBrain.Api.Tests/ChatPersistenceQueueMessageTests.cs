using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using ProjectBrain.Api.Background;
using ProjectBrain.Domain;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Api.Tests;

public class ChatPersistenceQueueMessageTests
{
    [Fact]
    public void ChatPersistenceQueueMessage_RoundTripsJson()
    {
        var original = new ChatPersistenceQueueMessage
        {
            SchemaVersion = "1",
            ConversationId = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
            UserId = "user-1",
            UserContent = "hello",
            AssistantContent = "hi there"
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(original, options);
        var back = JsonSerializer.Deserialize<ChatPersistenceQueueMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        back.Should().NotBeNull();
        back!.ConversationId.Should().Be(original.ConversationId);
        back.UserId.Should().Be("user-1");
        back.UserContent.Should().Be("hello");
        back.AssistantContent.Should().Be("hi there");
        back.SchemaVersion.Should().Be("1");
    }

    [Fact]
    public async Task NullChatPersistenceQueue_TryEnqueueAsync_ReturnsFalse()
    {
        var q = new NullChatPersistenceQueue();
        var ok = await q.TryEnqueueAsync(new ChatPersistenceQueueMessage(), CancellationToken.None);
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task ChatPersistenceHelper_EnqueueOrPersistAsync_WhenQueueReturnsFalse_CallsAddManyAndTrack()
    {
        var queue = new Mock<IChatPersistenceQueue>();
        queue.Setup(x => x.TryEnqueueAsync(It.IsAny<ChatPersistenceQueueMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var chat = new Mock<IChatService>();
        chat.Setup(x => x.AddMany(It.IsAny<List<ChatMessage>>())).ReturnsAsync(new List<ChatMessage>());

        var usage = new Mock<IUsageTrackingService>();
        usage.Setup(x => x.TrackAIQueryAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var unitOfWork = CreateUnitOfWorkMock();

        var convId = Guid.NewGuid();
        await ChatPersistenceHelper.EnqueueOrPersistAsync(
            queue.Object,
            chat.Object,
            usage.Object,
            unitOfWork.Object,
            convId,
            "uid",
            "u",
            "a",
            CancellationToken.None);

        chat.Verify(x => x.AddMany(It.Is<List<ChatMessage>>(l => l.Count == 2)), Times.Once);
        usage.Verify(x => x.TrackAIQueryAsync("uid"), Times.Once);
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChatPersistenceHelper_EnqueueOrPersistAsync_WhenQueueReturnsTrue_SkipsSyncPersistence()
    {
        var queue = new Mock<IChatPersistenceQueue>();
        queue.Setup(x => x.TryEnqueueAsync(It.IsAny<ChatPersistenceQueueMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var chat = new Mock<IChatService>();
        var usage = new Mock<IUsageTrackingService>();
        var unitOfWork = CreateUnitOfWorkMock();

        await ChatPersistenceHelper.EnqueueOrPersistAsync(
            queue.Object,
            chat.Object,
            usage.Object,
            unitOfWork.Object,
            Guid.NewGuid(),
            "uid",
            "u",
            "a",
            CancellationToken.None);

        chat.Verify(x => x.AddMany(It.IsAny<List<ChatMessage>>()), Times.Never);
        usage.Verify(x => x.TrackAIQueryAsync(It.IsAny<string>()), Times.Never);
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChatPersistenceHelper_PersistSynchronouslyAsync_WhenUsageTrackingFails_RollsBack()
    {
        var chat = new Mock<IChatService>();
        chat.Setup(x => x.AddMany(It.IsAny<List<ChatMessage>>())).ReturnsAsync(new List<ChatMessage>());

        var usage = new Mock<IUsageTrackingService>();
        usage.Setup(x => x.TrackAIQueryAsync("uid")).ThrowsAsync(new InvalidOperationException("usage failed"));
        var unitOfWork = CreateUnitOfWorkMock();

        var act = () => ChatPersistenceHelper.PersistSynchronouslyAsync(
            chat.Object,
            usage.Object,
            unitOfWork.Object,
            Guid.NewGuid(),
            "uid",
            "u",
            "a");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("usage failed");
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var transaction = new Mock<IDbContextTransaction>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return unitOfWork;
    }
}
