using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Database.Constants;

namespace ProjectBrain.Database.Tests;

public class CoachSpecialismOptionSeedingTests : IDisposable
{
    private readonly AppDbContext _context;

    public CoachSpecialismOptionSeedingTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockContextLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockContextLogger.Object);
    }

    [Fact]
    public async Task SeedCoachSpecialismOptions_ShouldCreateAllCatalogValues_WhenNoneExist()
    {
        var toAdd = CoachSpecialismCatalog.DefaultOptions
            .Select((name, index) => new CoachSpecialismOption
            {
                Name = name,
                SortOrder = index + 1,
                IsActive = true,
            })
            .ToList();

        await _context.CoachSpecialismOptions.AddRangeAsync(toAdd);
        await _context.SaveChangesAsync();

        var saved = await _context.CoachSpecialismOptions
            .OrderBy(o => o.SortOrder)
            .Select(o => o.Name)
            .ToListAsync();

        saved.Should().HaveCount(19);
        saved.Should().BeEquivalentTo(CoachSpecialismCatalog.DefaultOptions);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
