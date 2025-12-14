using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class UserServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IUserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);
        var repository = new UserRepository(_context);
        var unitOfWork = new UnitOfWork(_context);
        _userService = new UserService(repository, _context, unitOfWork);
    }

    [Fact]
    public async Task Create_ShouldAddUserToDatabase()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = "auth0|123456",
            Email = "test@example.com",
            FullName = "Test User",
            DoB = new DateOnly(1990, 1, 1),
            IsOnboarded = true
        };

        // Act
        var result = await _userService.Create(userDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userDto.Id);
        result.Email.Should().Be(userDto.Email);
        result.FullName.Should().Be(userDto.FullName);

        var savedUser = await _context.Users.FindAsync(userDto.Id);
        savedUser.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var user = new User
        {
            Id = "auth0|123456",
            Email = "test@example.com",
            FullName = "Test User",
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetById(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _userService.GetById("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var user = new User
        {
            Id = "auth0|123456",
            Email = "test@example.com",
            FullName = "Test User",
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetByEmail(user.Email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _userService.GetByEmail("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void User_FirstName_ShouldReturnFirstPartOfFullName()
    {
        // Arrange
        var user = new User
        {
            Id = "auth0|123456",
            Email = "test@example.com",
            FullName = "John Doe Smith",
        };

        // Assert
        user.FirstName.Should().Be("John");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
