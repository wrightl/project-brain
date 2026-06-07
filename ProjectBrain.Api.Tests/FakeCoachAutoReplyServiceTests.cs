using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Database;
using ProjectBrain.Database.Constants;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;

namespace ProjectBrain.Api.Tests;

public class FakeCoachAutoReplyServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly FakeCoachAutoReplyService _service;
    private readonly Connection _connection;
    private const string UserId = "auth0|user1";
    private const string TestCoachId = "auth0|coach-test";
    private const string RealCoachId = "auth0|coach-real";
    private static readonly DateTime UserMessageCreatedAt = new(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);

    public FakeCoachAutoReplyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockContextLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockContextLogger.Object);

        _context.Users.AddRange(
            new User
            {
                Id = UserId,
                Email = "testuser1@projectbrain.test",
                FullName = "Test User",
                EmailVerified = true,
            },
            new User
            {
                Id = TestCoachId,
                Email = "sarah.mitchell@projectbrain.test",
                FullName = "Sarah Mitchell",
                EmailVerified = true,
            },
            new User
            {
                Id = RealCoachId,
                Email = "real.coach@example.com",
                FullName = "Real Coach",
                EmailVerified = true,
            });
        _context.SaveChanges();

        _connection = new Connection
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            CoachId = TestCoachId,
            Status = "accepted",
            RequestedBy = "user",
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _context.Connections.Add(_connection);
        _context.SaveChanges();

        var coachMessageService = new CoachMessageService(_context);
        var userRepository = new UserRepository(_context);

        var configuration = BuildConfiguration(enabled: true);

        _service = new FakeCoachAutoReplyService(
            coachMessageService,
            userRepository,
            configuration);
    }

    [Fact]
    public async Task TryCreateAutoReplyAsync_CreatesReplyForTestCoachWhenUserSends()
    {
        var result = await _service.TryCreateAutoReplyAsync(_connection, UserId, UserMessageCreatedAt);

        result.Should().NotBeNull();
        result!.SenderId.Should().Be(TestCoachId);
        result.Content.Should().Be(FakeCoachEnvironment.DefaultMessage);
        result.MessageType.Should().Be("text");

        var messages = await _context.CoachMessages.ToListAsync();
        messages.Should().ContainSingle();
    }

    [Fact]
    public async Task TryCreateAutoReplyAsync_ReturnsNullWhenCoachSends()
    {
        var result = await _service.TryCreateAutoReplyAsync(_connection, TestCoachId, UserMessageCreatedAt);

        result.Should().BeNull();
        (await _context.CoachMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TryCreateAutoReplyAsync_ReturnsNullForRealCoach()
    {
        var realCoachConnection = new Connection
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            CoachId = RealCoachId,
            Status = "accepted",
            RequestedBy = "user",
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _context.Connections.Add(realCoachConnection);
        await _context.SaveChangesAsync();

        var result = await _service.TryCreateAutoReplyAsync(realCoachConnection, UserId, UserMessageCreatedAt);

        result.Should().BeNull();
        (await _context.CoachMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TryCreateAutoReplyAsync_ReturnsNullWhenDisabled()
    {
        var coachMessageService = new CoachMessageService(_context);
        var userRepository = new UserRepository(_context);
        var configuration = BuildConfiguration(enabled: false);

        var service = new FakeCoachAutoReplyService(
            coachMessageService,
            userRepository,
            configuration);

        var result = await service.TryCreateAutoReplyAsync(_connection, UserId, UserMessageCreatedAt);

        result.Should().BeNull();
        (await _context.CoachMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TryCreateAutoReplyAsync_UsesConfiguredMessage()
    {
        var customMessage = "Custom auto-reply message.";
        var coachMessageService = new CoachMessageService(_context);
        var userRepository = new UserRepository(_context);
        var configuration = BuildConfiguration(enabled: true, message: customMessage);

        var service = new FakeCoachAutoReplyService(
            coachMessageService,
            userRepository,
            configuration);

        var result = await service.TryCreateAutoReplyAsync(_connection, UserId, UserMessageCreatedAt);

        result.Should().NotBeNull();
        result!.Content.Should().Be(customMessage);
    }

    [Fact]
    public async Task TryCreateAutoReplyAsync_SetsCreatedAtAfterUserMessage()
    {
        var result = await _service.TryCreateAutoReplyAsync(_connection, UserId, UserMessageCreatedAt);

        result.Should().NotBeNull();
        result!.CreatedAt.Should().Be(UserMessageCreatedAt.AddMilliseconds(1));
    }

    [Fact]
    public void IsTestCoachEmail_IdentifiesTestCoachEmails()
    {
        TestUsers.IsTestCoachEmail("sarah.mitchell@projectbrain.test").Should().BeTrue();
        TestUsers.IsTestCoachEmail("real.coach@example.com").Should().BeFalse();
    }

    private static IConfiguration BuildConfiguration(
        bool enabled,
        string? message = null)
    {
        var data = new Dictionary<string, string?>
        {
            ["FakeCoachAutoReply:Enabled"] = enabled.ToString(),
        };
        if (message != null)
            data["FakeCoachAutoReply:Message"] = message;

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    public void Dispose() => _context.Dispose();
}
