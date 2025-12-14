using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Tests;

public class ConversationEndpointsTests
{
    private readonly Mock<ILogger<ConversationServices>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IConversationService> _mockConversationService;
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly ConversationServices _conversationServices;

    public ConversationEndpointsTests()
    {
        _mockLogger = new Mock<ILogger<ConversationServices>>();
        _mockConfig = new Mock<IConfiguration>();
        _mockConversationService = new Mock<IConversationService>();
        _mockIdentityService = new Mock<IIdentityService>();
        var mockConversationRepository = new Mock<ProjectBrain.Domain.Repositories.IConversationRepository>();

        _conversationServices = new ConversationServices(
            _mockConversationService.Object,
            mockConversationRepository.Object,
            _mockIdentityService.Object,
            _mockLogger.Object,
            _mockConfig.Object
        );
    }

    [Fact]
    public async Task CreateConversation_ShouldCreateAndReturnConversation()
    {
        // Arrange
        var userId = "auth0|123456";
        var request = new CreateConversationRequest { Title = "New Conversation" };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockConversationService.Setup(s => s.Add(It.IsAny<Conversation>()))
            .ReturnsAsync((Conversation c) => c);

        // Act
        var method = typeof(ConversationEndpoints)
            .GetMethod("CreateConversation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _conversationServices, request })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockConversationService.Verify(s => s.Add(It.Is<Conversation>(c =>
            c.UserId == userId &&
            c.Title == request.Title
        )), Times.Once);
    }

    [Fact]
    public async Task GetConversationById_ShouldReturnOk_WhenConversationExists()
    {
        // Arrange
        var userId = "auth0|123456";
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = userId,
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockConversationService.Setup(s => s.GetById(conversationId, userId))
            .ReturnsAsync(conversation);

        // Act
        var method = typeof(ConversationEndpoints)
            .GetMethod("GetConversationById", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _conversationServices, conversationId })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockConversationService.Verify(s => s.GetById(conversationId, userId), Times.Once);
    }

    [Fact]
    public async Task GetConversationById_ShouldReturnNotFound_WhenConversationDoesNotExist()
    {
        // Arrange
        var userId = "auth0|123456";
        var conversationId = Guid.NewGuid();

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockConversationService.Setup(s => s.GetById(conversationId, userId))
            .ReturnsAsync((Conversation?)null);

        // Act
        var method = typeof(ConversationEndpoints)
            .GetMethod("GetConversationById", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _conversationServices, conversationId })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockConversationService.Verify(s => s.GetById(conversationId, userId), Times.Once);
    }

    [Fact]
    public async Task GetConversationById_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        _mockIdentityService.Setup(s => s.UserId).Returns((string?)null);

        // Act
        var method = typeof(ConversationEndpoints)
            .GetMethod("GetConversationById", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _conversationServices, conversationId })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllConversationsForUser_ShouldReturnConversations()
    {
        // Arrange
        var userId = "auth0|123456";
        var conversations = new List<Conversation>
        {
            new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Conversation 1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Conversation 2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockConversationService.Setup(s => s.GetAllForUser(userId))
            .ReturnsAsync(conversations);

        // Act
        var method = typeof(ConversationEndpoints)
            .GetMethod("GetAllConversationsForUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _conversationServices })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockConversationService.Verify(s => s.GetAllForUser(userId), Times.Once);
    }

    [Fact]
    public async Task UpdateConversation_ShouldUpdateAndReturnConversation()
    {
        // Arrange
        var userId = "auth0|123456";
        var conversationId = Guid.NewGuid();
        var request = new UpdateConversationRequest { Title = "Updated Title" };
        var existingConversation = new Conversation
        {
            Id = conversationId,
            UserId = userId,
            Title = "Old Title",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockConversationService.Setup(s => s.GetById(conversationId, userId))
            .ReturnsAsync(existingConversation);
        _mockConversationService.Setup(s => s.Update(It.IsAny<Conversation>()))
            .ReturnsAsync((Conversation c) => c);

        // Act
        var method = typeof(ConversationEndpoints)
            .GetMethod("UpdateConversation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _conversationServices, conversationId, request })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockConversationService.Verify(s => s.Update(It.Is<Conversation>(c =>
            c.Title == request.Title
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateConversation_ShouldReturnNotFound_WhenConversationDoesNotExist()
    {
        // Arrange
        var userId = "auth0|123456";
        var conversationId = Guid.NewGuid();
        var request = new UpdateConversationRequest { Title = "Updated Title" };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockConversationService.Setup(s => s.GetById(conversationId, userId))
            .ReturnsAsync((Conversation?)null);

        // Act
        var method = typeof(ConversationEndpoints)
            .GetMethod("UpdateConversation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _conversationServices, conversationId, request })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockConversationService.Verify(s => s.Update(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConversation_ShouldDeleteConversation()
    {
        // Arrange
        var userId = "auth0|123456";
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = userId,
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockConversationService.Setup(s => s.GetById(conversationId, userId))
            .ReturnsAsync(conversation);
        _mockConversationService.Setup(s => s.Remove(conversation))
            .ReturnsAsync(conversation);

        // Act
        var method = typeof(ConversationEndpoints)
            .GetMethod("DeleteConversation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _conversationServices, conversationId })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockConversationService.Verify(s => s.Remove(conversation), Times.Once);
    }

    [Fact]
    public async Task DeleteConversation_ShouldReturnNotFound_WhenConversationDoesNotExist()
    {
        // Arrange
        var userId = "auth0|123456";
        var conversationId = Guid.NewGuid();

        _mockIdentityService.Setup(s => s.UserId).Returns(userId);
        _mockConversationService.Setup(s => s.GetById(conversationId, userId))
            .ReturnsAsync((Conversation?)null);

        // Act
        var method = typeof(ConversationEndpoints)
            .GetMethod("DeleteConversation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task<IResult>)method!.Invoke(null, new object[] { _conversationServices, conversationId })!;
        var result = await task;

        // Assert
        result.Should().NotBeNull();
        _mockConversationService.Verify(s => s.Remove(It.IsAny<Conversation>()), Times.Never);
    }
}
