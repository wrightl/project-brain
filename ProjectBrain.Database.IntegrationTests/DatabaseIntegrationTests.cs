using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MsSql;

namespace ProjectBrain.Database.IntegrationTests;

/// <summary>
/// Integration tests using a real SQL Server container via Testcontainers
/// </summary>
public class DatabaseIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer;
    private AppDbContext? _context;
    private IUserService? _userService;
    private IConversationService? _conversationService;
    private IChatService? _chatService;

    public DatabaseIntegrationTests()
    {
        _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();

        var connectionString = _msSqlContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);

        // Create database schema
        await _context.Database.EnsureCreatedAsync();

        _userService = new UserService(_context);
        _conversationService = new ConversationService(_context);
        _chatService = new ChatService(_context);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
        await _msSqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task UserService_WithRealDatabase_ShouldPersistAndRetrieveUser()
    {
        // Arrange
        var user = new User
        {
            Id = "real-db-user-1",
            Email = "realdb@example.com",
            FullName = "Real DB User",
            FavoriteColor = "Purple",
            DoB = new DateOnly(1995, 7, 10),
            IsOnboarded = true
        };

        // Act
        var created = await _userService!.Create(user);

        // Clear the context to ensure we're reading from database
        _context!.ChangeTracker.Clear();

        var retrieved = await _userService.GetById(user.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(user.Id);
        retrieved.Email.Should().Be(user.Email);
        retrieved.FullName.Should().Be(user.FullName);
        retrieved.FavoriteColor.Should().Be(user.FavoriteColor);
        retrieved.IsOnboarded.Should().BeTrue();
    }

    [Fact]
    public async Task ConversationService_WithRealDatabase_ShouldHandleRelationships()
    {
        // Arrange
        var userId = "integration-user-1";
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Integration Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Create conversation
        var created = await _conversationService!.Add(conversation);
        _context!.ChangeTracker.Clear();

        // Add messages
        var message1 = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = "Hello from integration test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var message2 = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = "Response from integration test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _chatService!.AddMany(new List<ChatMessage> { message1, message2 });
        _context.ChangeTracker.Clear();

        // Retrieve with messages
        var retrieved = await _conversationService.GetByIdWithMessages(conversation.Id, userId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Messages.Should().HaveCount(2);
        retrieved.Messages.Should().Contain(m => m.Role == "user" && m.Content == "Hello from integration test");
        retrieved.Messages.Should().Contain(m => m.Role == "assistant");
    }

    [Fact]
    public async Task CascadeDelete_ShouldDeleteMessagesWhenConversationDeleted()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = "cascade-test-user",
            Title = "Cascade Delete Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _conversationService!.Add(conversation);

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = "This message should be deleted",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _chatService!.Add(message);
        var messageId = message.Id;

        _context!.ChangeTracker.Clear();

        // Act - Delete conversation
        var conversationToDelete = await _context.Conversations.FindAsync(conversation.Id);
        await _conversationService.Remove(conversationToDelete!);

        _context.ChangeTracker.Clear();

        // Assert - Message should also be deleted due to cascade
        var deletedMessage = await _context.ChatMessages.FindAsync(messageId);
        deletedMessage.Should().BeNull();
    }

    [Fact]
    public async Task ComplexQuery_ShouldRetrieveMultipleConversationsWithFiltering()
    {
        // Arrange
        var userId = "multi-conv-user";

        var conversations = new List<Conversation>
        {
            new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "First Conversation",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Second Conversation",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow
            },
            new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = "different-user",
                Title = "Other User Conversation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var conv in conversations)
        {
            await _conversationService!.Add(conv);
        }

        _context!.ChangeTracker.Clear();

        // Act
        var retrieved = await _conversationService!.GetAllForUser(userId);

        // Assert
        var conversationList = retrieved.ToList();
        conversationList.Should().HaveCount(2);
        conversationList.Should().AllSatisfy(c => c.UserId.Should().Be(userId));
        conversationList[0].Title.Should().Be("Second Conversation"); // Most recent first
        conversationList[1].Title.Should().Be("First Conversation");
    }

    [Fact]
    public async Task Transaction_ShouldRollbackOnError()
    {
        // Arrange
        var user = new User
        {
            Id = "transaction-test",
            Email = "transaction@example.com",
            FullName = "Transaction Test",
            FavoriteColor = "Yellow",
            DoB = new DateOnly(1988, 12, 25),
            IsOnboarded = true
        };

        // Act & Assert
        await using var transaction = await _context!.Database.BeginTransactionAsync();

        try
        {
            await _userService!.Create(user);

            // Simulate an error - try to create duplicate
            var duplicate = new User
            {
                Id = user.Id, // Same ID will cause error
                Email = "different@example.com",
                FullName = "Different Name",
                FavoriteColor = "Orange",
                DoB = new DateOnly(1990, 1, 1),
                IsOnboarded = true
            };

            var act = async () => await _userService.Create(duplicate);
            await act.Should().ThrowAsync<Exception>();

            await transaction.RollbackAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
        }

        _context.ChangeTracker.Clear();

        // Verify rollback - user should not exist
        var retrieved = await _userService!.GetById(user.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldHandleMultipleOperations()
    {
        // Arrange
        var tasks = new List<Task>();
        var userId = "concurrent-user";

        // Act - Create multiple conversations concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = $"Concurrent Conversation {index}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _conversationService!.Add(conversation);
            }));
        }

        await Task.WhenAll(tasks);
        _context!.ChangeTracker.Clear();

        // Assert - All conversations should be created
        var allConversations = await _conversationService!.GetAllForUser(userId);
        allConversations.Should().HaveCount(10);
    }
}
