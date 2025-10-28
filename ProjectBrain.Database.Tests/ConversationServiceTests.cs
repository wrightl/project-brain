using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;

namespace ProjectBrain.Database.Tests;

public class ConversationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IConversationService _conversationService;
    private const string TestUserId = "auth0|test-user";
    private const string OtherUserId = "auth0|other-user";

    public ConversationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);
        _conversationService = new ConversationService(_context);
    }

    [Fact]
    public async Task Add_ShouldAddConversationToDatabase()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _conversationService.Add(conversation);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(conversation.Id);
        result.Title.Should().Be(conversation.Title);
        result.UserId.Should().Be(TestUserId);

        var savedConversation = await _context.Conversations.FindAsync(conversation.Id);
        savedConversation.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ShouldReturnConversation_WhenConversationExistsForUser()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _conversationService.GetById(conversation.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(conversation.Id);
        result.Title.Should().Be(conversation.Title);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenConversationDoesNotExist()
    {
        // Act
        var result = await _conversationService.GetById(Guid.NewGuid(), TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenConversationBelongsToDifferentUser()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _conversationService.GetById(conversation.Id, OtherUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdWithMessages_ShouldReturnConversationWithMessages()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Conversations.Add(conversation);

        var message1 = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = "Hello",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var message2 = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = "Hi there!",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ChatMessages.AddRange(message1, message2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _conversationService.GetByIdWithMessages(conversation.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(2);
        result.Messages.Should().Contain(m => m.Role == "user" && m.Content == "Hello");
        result.Messages.Should().Contain(m => m.Role == "assistant" && m.Content == "Hi there!");
    }

    [Fact]
    public async Task GetAllForUser_ShouldReturnAllConversationsForUser()
    {
        // Arrange
        var conversation1 = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Conversation 1",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var conversation2 = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Conversation 2",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
        var otherUserConversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = OtherUserId,
            Title = "Other User Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Conversations.AddRange(conversation1, conversation2, otherUserConversation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _conversationService.GetAllForUser(TestUserId);

        // Assert
        var conversations = result.ToList();
        conversations.Should().HaveCount(2);
        conversations.Should().AllSatisfy(c => c.UserId.Should().Be(TestUserId));
        conversations[0].Title.Should().Be("Conversation 2"); // Most recently updated first
        conversations[1].Title.Should().Be("Conversation 1");
    }

    [Fact]
    public async Task GetAllForUser_ShouldReturnEmptyList_WhenUserHasNoConversations()
    {
        // Act
        var result = await _conversationService.GetAllForUser(TestUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ShouldUpdateConversation()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Original Title",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Modify the conversation
        conversation.Title = "Updated Title";
        conversation.UpdatedAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = await _conversationService.Update(conversation);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");

        var updated = await _context.Conversations.FindAsync(conversation.Id);
        updated!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task Remove_ShouldDeleteConversation()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _conversationService.Remove(conversation);

        // Assert
        result.Should().NotBeNull();

        var deleted = await _context.Conversations.FindAsync(conversation.Id);
        deleted.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
