using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain.Repositories;

namespace ProjectBrain.Database.Tests;

public class UserErasureRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserErasureRepository _repository;

    public UserErasureRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, new Mock<ILogger<AppDbContext>>().Object);
        _repository = new UserErasureRepository(_context);
    }

    [Fact]
    public async Task DeleteCommunityDataAsync_RemovesRowsThatBlockUserDelete()
    {
        var user = new User { Id = "auth0|erase-me", Email = "erase@example.com", FullName = "Erase Me" };
        var other = new User { Id = "auth0|other", Email = "other@example.com", FullName = "Other" };
        var channel = new CommunityChannel
        {
            Id = Guid.NewGuid(),
            Slug = "wins",
            Name = "Wins",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var ownPost = new CommunityPost
        {
            Id = Guid.NewGuid(),
            ChannelId = channel.Id,
            AuthorUserId = user.Id,
            Body = "my post",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var otherPost = new CommunityPost
        {
            Id = Guid.NewGuid(),
            ChannelId = channel.Id,
            AuthorUserId = other.Id,
            Body = "their post",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Users.AddRange(user, other);
        _context.CommunityChannels.Add(channel);
        _context.CommunityPosts.AddRange(ownPost, otherPost);
        _context.CommunityReactions.AddRange(
            new CommunityReaction
            {
                Id = Guid.NewGuid(),
                PostId = otherPost.Id,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            },
            new CommunityReaction
            {
                Id = Guid.NewGuid(),
                PostId = ownPost.Id,
                UserId = other.Id,
                CreatedAt = DateTime.UtcNow,
            });
        _context.CommunityReports.AddRange(
            new CommunityReport
            {
                Id = Guid.NewGuid(),
                PostId = otherPost.Id,
                ReporterUserId = user.Id,
                Reason = "spam",
                Status = CommunityReport.StatusOpen,
                CreatedAt = DateTime.UtcNow,
            },
            new CommunityReport
            {
                Id = Guid.NewGuid(),
                PostId = ownPost.Id,
                ReporterUserId = other.Id,
                Reason = "abuse",
                Status = CommunityReport.StatusOpen,
                CreatedAt = DateTime.UtcNow,
            });
        await _context.SaveChangesAsync();

        await _repository.DeleteCommunityDataAsync(user.Id);

        (await _context.CommunityPosts.AnyAsync(p => p.AuthorUserId == user.Id)).Should().BeFalse();
        (await _context.CommunityReactions.AnyAsync(r => r.UserId == user.Id || r.PostId == ownPost.Id)).Should().BeFalse();
        (await _context.CommunityReports.AnyAsync(r => r.ReporterUserId == user.Id || r.PostId == ownPost.Id)).Should().BeFalse();
        (await _context.CommunityPosts.AnyAsync(p => p.Id == otherPost.Id)).Should().BeTrue();

        var trackedUser = await _context.Users.SingleAsync(u => u.Id == user.Id);
        _context.Users.Remove(trackedUser);
        var act = () => _context.SaveChangesAsync();
        await act.Should().NotThrowAsync();
        (await _context.Users.AnyAsync(u => u.Id == user.Id)).Should().BeFalse();
        (await _context.Users.AnyAsync(u => u.Id == other.Id)).Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
