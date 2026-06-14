using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Moq;

namespace ProjectBrain.Database.Tests;

public class CountriesMigrationTests : IDisposable
{
    private readonly AppDbContext _context;

    public CountriesMigrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockContextLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockContextLogger.Object);
    }

    [Fact]
    public void AddCountriesTable_ShouldBeRegisteredForAppDbContext()
    {
        var migrationType = typeof(AppDbContext).Assembly
            .GetTypes()
            .Single(t => t.Name == "AddCountriesTable" && typeof(Migration).IsAssignableFrom(t));

        var dbContextAttr = migrationType.GetCustomAttributes(typeof(DbContextAttribute), inherit: false)
            .Cast<DbContextAttribute>()
            .SingleOrDefault();

        var migrationAttr = migrationType.GetCustomAttributes(typeof(MigrationAttribute), inherit: false)
            .Cast<MigrationAttribute>()
            .SingleOrDefault();

        dbContextAttr.Should().NotBeNull();
        dbContextAttr!.ContextType.Should().Be(typeof(AppDbContext));
        migrationAttr.Should().NotBeNull();
        migrationAttr!.Id.Should().Be("20260614120000_AddCountriesTable");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
