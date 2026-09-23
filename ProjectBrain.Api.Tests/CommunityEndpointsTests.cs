using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Constants;
using ProjectBrain.Shared.Dtos.Community;

namespace ProjectBrain.Api.Tests;

public class CommunityEndpointsTests
{
    private readonly Mock<ICommunityService> _communityService = new();
    private readonly Mock<IFeatureFlagService> _featureFlags = new();
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<ILogger<CommunityServices>> _logger = new();
    private readonly CommunityServices _services;

    public CommunityEndpointsTests()
    {
        _services = new CommunityServices(
            _communityService.Object,
            _featureFlags.Object,
            _identity.Object,
            _httpContextAccessor.Object,
            _logger.Object);

        SetUserRole(AppRoles.User);
        _identity.Setup(i => i.UserId).Returns("auth0|user1");
        _identity.Setup(i => i.IsAdmin).Returns(false);
    }

    private void SetUserRole(string role)
    {
        var identity = new ClaimsIdentity(
            [new Claim(AuthClaimTypes.Roles, role)],
            authenticationType: "Test");
        _httpContextAccessor.Setup(a => a.HttpContext)
            .Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });
    }

    private static MethodInfo Method(string name) =>
        typeof(CommunityEndpoints).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Method {name} not found");

    [Fact]
    public async Task GetChannels_WhenFlagOff_ThrowsNotFound()
    {
        _featureFlags
            .Setup(f => f.IsFeatureEnabled(FeatureFlags.CommunityFeatureEnabled))
            .ReturnsAsync(false);

        var task = (Task<IResult>)Method("GetChannels").Invoke(null, [_services])!;

        var ex = await Assert.ThrowsAsync<AppException>(() => task);
        ex.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetChannels_WhenFlagOn_ReturnsChannels()
    {
        _featureFlags
            .Setup(f => f.IsFeatureEnabled(FeatureFlags.CommunityFeatureEnabled))
            .ReturnsAsync(true);
        _communityService
            .Setup(s => s.GetChannelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CommunityChannelDto
                {
                    Id = Guid.NewGuid(),
                    Slug = "wins",
                    Name = "Wins",
                    SortOrder = 1,
                }
            ]);

        var task = (Task<IResult>)Method("GetChannels").Invoke(null, [_services])!;
        var result = await task;

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<IReadOnlyList<CommunityChannelDto>>>();
    }

    [Fact]
    public async Task GetChannels_WhenCoachRole_ThrowsForbidden()
    {
        SetUserRole(AppRoles.Coach);
        _featureFlags
            .Setup(f => f.IsFeatureEnabled(FeatureFlags.CommunityFeatureEnabled))
            .ReturnsAsync(true);

        var task = (Task<IResult>)Method("GetChannels").Invoke(null, [_services])!;

        var ex = await Assert.ThrowsAsync<AppException>(() => task);
        ex.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetLatestFeed_WhenFlagOn_ReturnsPage()
    {
        _featureFlags
            .Setup(f => f.IsFeatureEnabled(FeatureFlags.CommunityFeatureEnabled))
            .ReturnsAsync(true);
        _communityService
            .Setup(s => s.GetLatestFeedAsync(null, 20, "auth0|user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommunityPostsPageDto { Items = [], NextCursor = null });

        var task = (Task<IResult>)Method("GetLatestFeed").Invoke(null, [_services, null, null])!;
        var result = await task;

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<CommunityPostsPageDto>>();
    }

    [Fact]
    public async Task HidePost_WhenAdmin_WorksEvenIfFlagOff()
    {
        _featureFlags
            .Setup(f => f.IsFeatureEnabled(FeatureFlags.CommunityFeatureEnabled))
            .ReturnsAsync(false);
        _identity.Setup(i => i.IsAdmin).Returns(true);
        var postId = Guid.NewGuid();

        var task = (Task<IResult>)Method("HidePost").Invoke(null, [_services, postId])!;
        var result = await task;

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        _communityService.Verify(s => s.HidePostAsync(postId, "auth0|user1", It.IsAny<CancellationToken>()), Times.Once);
        _featureFlags.Verify(f => f.IsFeatureEnabled(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeletePost_WhenFlagOn_DelegatesToService()
    {
        _featureFlags
            .Setup(f => f.IsFeatureEnabled(FeatureFlags.CommunityFeatureEnabled))
            .ReturnsAsync(true);
        var postId = Guid.NewGuid();

        var task = (Task<IResult>)Method("DeletePost").Invoke(null, [_services, postId])!;
        var result = await task;

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        _communityService.Verify(s => s.DeleteOwnPostAsync(postId, "auth0|user1", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class CommunityServiceTests
{
    private readonly Mock<ICommunityChannelRepository> _channels = new();
    private readonly Mock<ICommunityPostRepository> _posts = new();
    private readonly Mock<ICommunityReactionRepository> _reactions = new();
    private readonly Mock<ICommunityReportRepository> _reports = new();
    private readonly Mock<ProjectBrain.Domain.UnitOfWork.IUnitOfWork> _uow = new();
    private readonly CommunityService _sut;

    public CommunityServiceTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sut = new CommunityService(
            _channels.Object,
            _posts.Object,
            _reactions.Object,
            _reports.Object,
            _uow.Object);
    }

    [Fact]
    public async Task DeleteOwnPostAsync_ThrowsForbidden_WhenNotAuthor()
    {
        var postId = Guid.NewGuid();
        _posts.Setup(p => p.GetByIdAsync(postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectBrain.Database.Models.CommunityPost
            {
                Id = postId,
                AuthorUserId = "auth0|other",
                Body = "hi",
            });

        var act = () => _sut.DeleteOwnPostAsync(postId, "auth0|me");

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteOwnPostAsync_SoftDeletes_WhenAuthor()
    {
        var postId = Guid.NewGuid();
        var post = new ProjectBrain.Database.Models.CommunityPost
        {
            Id = postId,
            AuthorUserId = "auth0|me",
            Body = "hi",
        };
        _posts.Setup(p => p.GetByIdAsync(postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        await _sut.DeleteOwnPostAsync(postId, "auth0|me");

        post.DeletedAt.Should().NotBeNull();
        _posts.Verify(p => p.Update(post), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddReactionAsync_IsIdempotent_WhenAlreadyReacted()
    {
        var postId = Guid.NewGuid();
        _posts.Setup(p => p.GetByIdAsync(postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectBrain.Database.Models.CommunityPost
            {
                Id = postId,
                AuthorUserId = "auth0|author",
                Body = "hi",
            });
        _reactions.Setup(r => r.GetByPostAndUserAsync(postId, "auth0|me", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectBrain.Database.Models.CommunityReaction
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = "auth0|me",
            });

        await _sut.AddReactionAsync(postId, "auth0|me");

        _reactions.Verify(r => r.Add(It.IsAny<ProjectBrain.Database.Models.CommunityReaction>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HidePostAsync_MarksHidden()
    {
        var postId = Guid.NewGuid();
        var post = new ProjectBrain.Database.Models.CommunityPost
        {
            Id = postId,
            AuthorUserId = "auth0|author",
            Body = "spam",
        };
        _posts.Setup(p => p.GetByIdAsync(postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        await _sut.HidePostAsync(postId, "auth0|admin");

        post.IsHidden.Should().BeTrue();
        post.HiddenByUserId.Should().Be("auth0|admin");
        post.HiddenAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPostsAsync_ExcludesHiddenViaRepositoryFilter()
    {
        var channelId = Guid.NewGuid();
        _channels.Setup(c => c.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _channels.Setup(c => c.GetBySlugAsync("wins", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectBrain.Database.Models.CommunityChannel
            {
                Id = channelId,
                Slug = "wins",
                Name = "Wins",
                IsActive = true,
            });
        _posts.Setup(p => p.GetPagedByChannelAsync(
                channelId, null, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<ProjectBrain.Database.Models.CommunityPost>)[],
                false));
        _reactions.Setup(r => r.CountByPostIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        _reactions.Setup(r => r.GetReactedPostIdsAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var page = await _sut.GetPostsAsync("wins", null, 20, "auth0|me");

        page.Items.Should().BeEmpty();
        _posts.Verify(p => p.GetPagedByChannelAsync(
            channelId, null, null, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLatestFeedAsync_ReturnsMappedPosts_AndNextCursor()
    {
        var postId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        _channels.Setup(c => c.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _posts.Setup(p => p.GetPagedLatestAsync(null, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<ProjectBrain.Database.Models.CommunityPost>)
                [
                    new ProjectBrain.Database.Models.CommunityPost
                    {
                        Id = postId,
                        ChannelId = Guid.NewGuid(),
                        AuthorUserId = "auth0|author",
                        Body = "Hello feed",
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt,
                        Channel = new ProjectBrain.Database.Models.CommunityChannel
                        {
                            Slug = "wins",
                            Name = "Wins",
                            IsActive = true,
                        },
                        Author = new User
                        {
                            Id = "auth0|author",
                            Email = "author@example.com",
                            FullName = "Author",
                        },
                    }
                ],
                true));
        _reactions.Setup(r => r.CountByPostIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [postId] = 2 });
        _reactions.Setup(r => r.GetReactedPostIdsAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var page = await _sut.GetLatestFeedAsync(null, 20, "auth0|me");

        page.Items.Should().HaveCount(1);
        page.Items[0].Body.Should().Be("Hello feed");
        page.Items[0].ChannelSlug.Should().Be("wins");
        page.Items[0].ReactionCount.Should().Be(2);
        page.NextCursor.Should().NotBeNullOrEmpty();
        _posts.Verify(p => p.GetPagedLatestAsync(null, null, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
