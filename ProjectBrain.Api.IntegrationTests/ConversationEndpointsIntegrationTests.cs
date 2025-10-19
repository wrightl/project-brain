using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectBrain.Api.IntegrationTests;

public class ConversationEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private const string TestUserId = "test-user-123";

    public ConversationEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task CreateConversation_ShouldReturnCreated()
    {
        // Arrange
        var request = new { Title = "Test Conversation" };

        // Act
        var response = await _client.PostAsJsonAsync("/conversation/", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var locationHeader = response.Headers.Location;
        locationHeader.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConversationById_ShouldReturnConversation_WhenExists()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Existing Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/conversation/{conversation.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Existing Conversation");
    }

    [Fact]
    public async Task GetConversationById_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/conversation/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllConversationsForUser_ShouldReturnOnlyUserConversations()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Clear existing conversations
        context.Conversations.RemoveRange(context.Conversations);
        await context.SaveChangesAsync();

        var userConversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "My Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var otherConversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = "other-user",
            Title = "Other User Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Conversations.AddRange(userConversation, otherConversation);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/conversation/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("My Conversation");
        content.Should().NotContain("Other User Conversation");
    }

    [Fact]
    public async Task UpdateConversation_ShouldUpdateTitle()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Original Title",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var updateRequest = new { Title = "Updated Title" };

        // Act
        var response = await _client.PutAsJsonAsync($"/conversation/{conversation.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in database
        var updated = await context.Conversations.FindAsync(conversation.Id);
        updated!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteConversation_ShouldRemoveFromDatabase()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "To Be Deleted",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/conversation/{conversation.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var deleted = await context.Conversations.FindAsync(conversation.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetConversationById_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var anonymousClient = _factory.CreateClient();
        var conversationId = Guid.NewGuid();

        // Act
        var response = await anonymousClient.GetAsync($"/conversation/{conversationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteWorkflow_CreateUpdateDelete_ShouldWorkEndToEnd()
    {
        // Create
        var createResponse = await _client.PostAsJsonAsync("/conversation/", new { Title = "Workflow Test" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var location = createResponse.Headers.Location!.ToString();
        var conversationId = location.Split('/').Last();

        // Read
        var getResponse = await _client.GetAsync($"/conversation/{conversationId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain("Workflow Test");

        // Update
        var updateResponse = await _client.PutAsJsonAsync($"/conversation/{conversationId}", new { Title = "Workflow Updated" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify update
        var verifyResponse = await _client.GetAsync($"/conversation/{conversationId}");
        var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
        verifyContent.Should().Contain("Workflow Updated");

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/conversation/{conversationId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var deletedResponse = await _client.GetAsync($"/conversation/{conversationId}");
        deletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
