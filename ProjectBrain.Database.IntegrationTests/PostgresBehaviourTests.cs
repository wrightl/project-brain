using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;
using ProjectBrain.MigrationService.Seeding;
using Testcontainers.PostgreSql;

namespace ProjectBrain.Database.IntegrationTests;

/// <summary>
/// Covers behaviour that changed when moving from SQL Server to Postgres:
/// case sensitivity, raw SQL, identity sequences and DateTime kinds.
/// </summary>
public class PostgresBehaviourTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:17").Build();
    private AppDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _context = CreateContext();
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    private AppDbContext CreateContext(bool retryOnFailure = false)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString(), npgsql =>
            {
                // Aspire's AddNpgsqlDbContext enables this by default
                if (retryOnFailure) npgsql.EnableRetryOnFailure();
            })
            .Options;
        return new AppDbContext(options, NullLogger<AppDbContext>.Instance);
    }

    private async Task<User> AddUserAsync(string id, string email)
    {
        var user = new User { Id = id, Email = email, FullName = "Test User" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetByEmailAsync_IgnoresCase()
    {
        await AddUserAsync("email-user", "Mixed.Case@Example.com");

        var found = await new UserRepository(_context).GetByEmailAsync("mixed.case@example.COM");

        found.Should().NotBeNull();
        found!.Id.Should().Be("email-user");
    }

    [Fact]
    public async Task GetByEmailAsync_TreatsUnderscoreLiterally()
    {
        await AddUserAsync("underscore-user", "first_last@example.com");

        var found = await new UserRepository(_context).GetByEmailAsync("firstxlast@example.com");

        found.Should().BeNull();
    }

    [Fact]
    public async Task SearchActiveByContentAsync_IgnoresCase()
    {
        await AddUserAsync("fact-user", "fact@example.com");
        _context.UserFacts.Add(new UserFact
        {
            UserId = "fact-user",
            Content = "Prefers Morning Walks before work",
            ContentHash = "hash-1",
            Status = MemoryStatuses.Active
        });
        await _context.SaveChangesAsync();

        var results = await new UserFactRepository(_context).SearchActiveByContentAsync("fact-user", "morning walks", 10);

        results.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordUserActivityAsync_UpdatesLastActivityAt()
    {
        await AddUserAsync("activity-user", "activity@example.com");
        var service = new UserActivityService(
            Mock.Of<IDistributedCache>(),
            _context,
            Mock.Of<IUserActivitySyncTrigger>(),
            NullLogger<UserActivityService>.Instance);

        await service.RecordUserActivityAsync("activity-user");

        await using var readContext = CreateContext();
        var user = await readContext.Users.SingleAsync(u => u.Id == "activity-user");
        user.LastActivityAt.Should().NotBeNull();
        user.LastActivityAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task JournalEntryService_AddAndUpdate_WorkWithRetryingExecutionStrategy()
    {
        await AddUserAsync("journal-user", "journal@example.com");
        await using var context = CreateContext(retryOnFailure: true);
        var service = new JournalEntryService(
            new JournalEntryRepository(context),
            new TagRepository(context),
            context,
            new UnitOfWork(context));

        var entry = await service.Add(new JournalEntry { Id = Guid.NewGuid(), UserId = "journal-user", Content = "First draft" });
        entry.Content = "Edited";
        await service.Update(entry);

        await using var readContext = CreateContext();
        var saved = await readContext.JournalEntries.SingleAsync(j => j.Id == entry.Id);
        saved.Content.Should().Be("Edited");
    }

    [Fact]
    public async Task SeedSubscriptionTiersAsync_LeavesSequenceAfterSeededIds()
    {
        await CriticalDatabaseSeeder.SeedSubscriptionTiersAsync(_context, NullLogger.Instance);

        _context.SubscriptionTiers.Add(new SubscriptionTier { Name = "Extra", UserType = "user" });
        await _context.SaveChangesAsync();

        (await _context.SubscriptionTiers.MaxAsync(t => t.Id)).Should().Be(6);
    }

    [Fact]
    public async Task SaveChanges_AcceptsUnspecifiedAndLocalDateTimes_AndReadsBackUtc()
    {
        var unspecified = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Unspecified);
        var local = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Local);
        _context.CommunityChannels.Add(new CommunityChannel
        {
            Slug = "dates",
            Name = "Dates",
            CreatedAt = unspecified,
            UpdatedAt = local
        });
        await _context.SaveChangesAsync();

        await using var readContext = CreateContext();
        var channel = await readContext.CommunityChannels.SingleAsync(c => c.Slug == "dates");
        channel.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        channel.CreatedAt.Should().Be(DateTime.SpecifyKind(unspecified, DateTimeKind.Utc));
        channel.UpdatedAt.Should().Be(local.ToUniversalTime());
    }

    [Fact]
    public async Task Query_WithUnspecifiedDateTimeParameter_DoesNotThrow()
    {
        var monthStart = new DateTime(2026, 1, 1);

        var act = () => _context.CommunityChannels.Where(c => c.CreatedAt >= monthStart).ToListAsync();

        await act.Should().NotThrowAsync();
    }
}
