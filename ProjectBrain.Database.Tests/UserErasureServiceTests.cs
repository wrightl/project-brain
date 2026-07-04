using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class UserErasureServiceTests
{
    [Fact]
    public async Task EraseUserAsync_DeletesSearchBlobsAndUserRow()
    {
        const string userId = "auth0|erase-me";
        var searchErasure = new Mock<IUserSearchIndexErasureService>();
        searchErasure
            .Setup(s => s.DeleteAllDocumentsForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        var blobErasure = new Mock<IUserBlobErasureService>();
        blobErasure
            .Setup(b => b.DeleteAllUserFilesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var auditRepo = new Mock<IMemoryPromotionAuditRepository>();
        auditRepo.Setup(r => r.DeleteByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var quizRepo = new Mock<IQuizResponseRepository>();
        quizRepo.Setup(r => r.DeleteByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var userErasureRepo = new Mock<IUserErasureRepository>();
        userErasureRepo
            .Setup(r => r.DeleteRelationalDataAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var factRepo = new Mock<IUserFactRepository>();
        factRepo
            .Setup(r => r.GetForUserByStatusesAsync(userId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFact>());

        var episodeRepo = new Mock<IUserEpisodeRepository>();
        episodeRepo
            .Setup(r => r.GetForUserByStatusesAsync(userId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserEpisode>());

        var memoryIndex = new Mock<IUserMemoryIndexService>();
        memoryIndex
            .Setup(m => m.DeleteAllForUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var userService = new Mock<IUserService>();
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var subscription = new Mock<ISubscriptionService>();
        subscription
            .Setup(s => s.GetUserSubscriptionAsync(userId, UserType.User))
            .ReturnsAsync((UserSubscription?)null);
        subscription
            .Setup(s => s.GetUserSubscriptionAsync(userId, UserType.Coach))
            .ReturnsAsync((UserSubscription?)null);

        var transaction = new Mock<IDbContextTransaction>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        unitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new UserErasureService(
            subscription.Object,
            searchErasure.Object,
            blobErasure.Object,
            auditRepo.Object,
            quizRepo.Object,
            userErasureRepo.Object,
            factRepo.Object,
            episodeRepo.Object,
            memoryIndex.Object,
            userService.Object,
            unitOfWork.Object,
            cache.Object,
            new Mock<ILogger<UserErasureService>>().Object);

        var result = await service.EraseUserAsync(userId);

        result.SearchDocumentsDeleted.Should().Be(12);
        result.BlobFilesDeleted.Should().Be(3);
        result.UserRowDeleted.Should().BeTrue();
        userService.Verify(u => u.DeleteById(userId), Times.Once);
        userErasureRepo.Verify(r => r.DeleteRelationalDataAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        searchErasure.Verify(s => s.DeleteAllDocumentsForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
