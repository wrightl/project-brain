using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using ProjectBrain.Database;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Api.Tests;

public class FakeCoachAutoAcceptServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ConnectionService _connectionService;
    private readonly FakeCoachAutoAcceptService _service;
    private const string UserId = "auth0|user1";
    private const string TestCoachId = "auth0|coach-test";

    public FakeCoachAutoAcceptServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, Mock.Of<Microsoft.Extensions.Logging.ILogger<AppDbContext>>());
        var repository = new ConnectionRepository(_context);
        var unitOfWork = new UnitOfWork(_context);
        _connectionService = new ConnectionService(repository, _context, unitOfWork);

        _service = new FakeCoachAutoAcceptService(
            _connectionService,
            BuildConfiguration(enabled: true));
    }

    [Fact]
    public async Task TryAutoAcceptAsync_AcceptsPendingConnectionForTestCoach()
    {
        var connection = await CreatePendingConnectionAsync();

        var result = await _service.TryAutoAcceptAsync(connection, "sarah.mitchell@projectbrain.test");

        result.Should().NotBeNull();
        result!.Status.Should().Be("accepted");
        result.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TryAutoAcceptAsync_ReturnsNullForRealCoach()
    {
        var connection = await CreatePendingConnectionAsync();

        var result = await _service.TryAutoAcceptAsync(connection, "real.coach@example.com");

        result.Should().BeNull();
        (await _connectionService.GetConnectionAsync(UserId, TestCoachId))!.Status.Should().Be("pending");
    }

    [Fact]
    public async Task TryAutoAcceptAsync_ReturnsNullWhenAlreadyAccepted()
    {
        var connection = await CreatePendingConnectionAsync();
        await _connectionService.AcceptConnectionAsync(UserId, TestCoachId);
        var accepted = await _connectionService.GetConnectionAsync(UserId, TestCoachId);

        var result = await _service.TryAutoAcceptAsync(accepted!, "sarah.mitchell@projectbrain.test");

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryAutoAcceptAsync_ReturnsNullWhenDisabled()
    {
        var connection = await CreatePendingConnectionAsync();
        var service = new FakeCoachAutoAcceptService(
            _connectionService,
            BuildConfiguration(enabled: false));

        var result = await service.TryAutoAcceptAsync(connection, "sarah.mitchell@projectbrain.test");

        result.Should().BeNull();
    }

    private async Task<Connection> CreatePendingConnectionAsync()
    {
        var connection = new Connection
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            CoachId = TestCoachId,
            Status = "pending",
            RequestedBy = "user",
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _context.Connections.Add(connection);
        await _context.SaveChangesAsync();
        return connection;
    }

    private static IConfiguration BuildConfiguration(bool enabled)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FakeCoachAutoReply:Enabled"] = enabled.ToString(),
            })
            .Build();
    }

    public void Dispose() => _context.Dispose();
}
