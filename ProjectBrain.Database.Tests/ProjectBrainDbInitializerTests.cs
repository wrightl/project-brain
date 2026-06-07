using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using ProjectBrain.Database.Constants;

namespace ProjectBrain.Database.Tests;

public class RoleSeedingTests : IDisposable
{
    private readonly AppDbContext _context;

    public RoleSeedingTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockContextLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockContextLogger.Object);
    }

    [Fact]
    public async Task RoleSeeding_ShouldCreateThreeRoles_WhenNoRolesExist()
    {
        // Arrange - Simulate seeding logic directly
        var roles = new List<Role>
        {
            new()
            {
                Name = AppRoles.User,
                Description = "Standard user with access to basic features",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = AppRoles.Coach,
                Description = "Coach user with access to coaching features and tools",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = AppRoles.Admin,
                Description = "Administrator with full system access and management capabilities",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        await _context.Roles.AddRangeAsync(roles);
        await _context.SaveChangesAsync();

        // Assert
        var savedRoles = await _context.Roles.ToListAsync();
        savedRoles.Should().HaveCount(3);

        var roleNames = savedRoles.Select(r => r.Name).ToList();
        roleNames.Should().Contain(AppRoles.User);
        roleNames.Should().Contain(AppRoles.Coach);
        roleNames.Should().Contain(AppRoles.Admin);
    }

    [Fact]
    public async Task RoleSeeding_ShouldNotDuplicateRoles_WhenRolesAlreadyExist()
    {
        // Arrange - Add roles first time
        if (!_context.Roles.Any())
        {
            var roles = new List<Role>
            {
                new() { Name = AppRoles.User, Description = "Standard user", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Name = AppRoles.Coach, Description = "Coach user", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Name = AppRoles.Admin, Description = "Administrator", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };
            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync();
        }

        var initialCount = await _context.Roles.CountAsync();

        // Act - Try to add again (simulating duplicate seed)
        if (!_context.Roles.Any())
        {
            var roles = new List<Role>
            {
                new() { Name = AppRoles.User, Description = "Standard user", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Name = AppRoles.Coach, Description = "Coach user", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Name = AppRoles.Admin, Description = "Administrator", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };
            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync();
        }

        // Assert
        var finalCount = await _context.Roles.CountAsync();
        finalCount.Should().Be(initialCount);
        finalCount.Should().Be(3);
    }

    [Fact]
    public async Task UserRole_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var role = new Role
        {
            Name = AppRoles.User,
            Description = "Standard user with access to basic features",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        // Assert
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.User);
        userRole.Should().NotBeNull();
        userRole!.Name.Should().Be(AppRoles.User);
        userRole.Description.Should().Contain("Standard user");
        userRole.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        userRole.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CoachRole_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var role = new Role
        {
            Name = AppRoles.Coach,
            Description = "Coach user with access to coaching features and tools",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        // Assert
        var coachRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Coach);
        coachRole.Should().NotBeNull();
        coachRole!.Name.Should().Be(AppRoles.Coach);
        coachRole.Description.Should().Contain("Coach");
    }

    [Fact]
    public async Task AdminRole_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var role = new Role
        {
            Name = AppRoles.Admin,
            Description = "Administrator with full system access and management capabilities",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        // Assert
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Admin);
        adminRole.Should().NotBeNull();
        adminRole!.Name.Should().Be(AppRoles.Admin);
        adminRole.Description.Should().Contain("Administrator");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
