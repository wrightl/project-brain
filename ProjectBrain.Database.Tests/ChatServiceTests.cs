using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ProjectBrain.Database.Tests;

public class ChatServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IChatService _chatService;
    private readonly Guid _testConversationId;

    public ChatServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);
        _chatService = new ChatService(_context);

        // Create a test conversation
        _testConversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = _testConversationId,
            UserId = "auth0|test-user",
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Conversations.Add(conversation);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Add_ShouldAddSingleChatMessageToDatabase()
    {
        // Arrange
        var chatMessage = new ChatMessage
        {
            ConversationId = _testConversationId,
            Role = "user",
            Content = "Hello, how are you?",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _chatService.Add(chatMessage);

        // Assert
        result.Should().NotBeNull();
        result.ConversationId.Should().Be(_testConversationId);
        result.Role.Should().Be("user");
        result.Content.Should().Be("Hello, how are you?");
        result.Id.Should().BeGreaterThan(0);

        var savedMessage = await _context.ChatMessages.FindAsync(result.Id);
        savedMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task Add_ShouldAddAssistantMessage()
    {
        // Arrange
        var chatMessage = new ChatMessage
        {
            ConversationId = _testConversationId,
            Role = "assistant",
            Content = "I'm doing well, thank you!",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _chatService.Add(chatMessage);

        // Assert
        result.Should().NotBeNull();
        result.Role.Should().Be("assistant");
        result.Content.Should().Be("I'm doing well, thank you!");
    }

    [Fact]
    public async Task AddMany_ShouldAddMultipleChatMessagesToDatabase()
    {
        // Arrange
        var chatMessages = new List<ChatMessage>
        {
            new ChatMessage
            {
                ConversationId = _testConversationId,
                Role = "user",
                Content = "First message",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ChatMessage
            {
                ConversationId = _testConversationId,
                Role = "assistant",
                Content = "First response",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ChatMessage
            {
                ConversationId = _testConversationId,
                Role = "user",
                Content = "Second message",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = await _chatService.AddMany(chatMessages);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(m => m.Id.Should().BeGreaterThan(0));

        var savedMessages = await _context.ChatMessages
            .Where(m => m.ConversationId == _testConversationId)
            .ToListAsync();
        savedMessages.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddMany_ShouldPreserveMessageOrder()
    {
        // Arrange
        var chatMessages = new List<ChatMessage>
        {
            new ChatMessage
            {
                ConversationId = _testConversationId,
                Role = "user",
                Content = "Message 1",
                CreatedAt = DateTime.UtcNow.AddSeconds(-2),
                UpdatedAt = DateTime.UtcNow.AddSeconds(-2)
            },
            new ChatMessage
            {
                ConversationId = _testConversationId,
                Role = "assistant",
                Content = "Message 2",
                CreatedAt = DateTime.UtcNow.AddSeconds(-1),
                UpdatedAt = DateTime.UtcNow.AddSeconds(-1)
            },
            new ChatMessage
            {
                ConversationId = _testConversationId,
                Role = "user",
                Content = "Message 3",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = await _chatService.AddMany(chatMessages);

        // Assert
        result[0].Content.Should().Be("Message 1");
        result[1].Content.Should().Be("Message 2");
        result[2].Content.Should().Be("Message 3");
    }

    [Fact]
    public async Task AddMany_ShouldHandleEmptyList()
    {
        // Arrange
        var chatMessages = new List<ChatMessage>();

        // Act
        var result = await _chatService.AddMany(chatMessages);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_ShouldSetDefaultEmptyStatus()
    {
        // Arrange
        var chatMessage = new ChatMessage
        {
            ConversationId = _testConversationId,
            Role = "user",
            Content = "Test message",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _chatService.Add(chatMessage);

        // Assert
        result.Status.Should().Be(string.Empty);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
