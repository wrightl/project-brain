using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Azure.Storage.Blobs;

namespace ProjectBrain.Api.Tests;

public class StorageLocationTests
{
    private static Storage CreateStorage()
    {
        return new Storage(
            Mock.Of<IConfiguration>(),
            new BlobServiceClient(new Uri("https://example.blob.core.windows.net")),
            Mock.Of<ILogger<Storage>>(),
            Mock.Of<ISearchIndexService>());
    }

    [Fact]
    public void DetermineLocation_SharedFiles_IncludeStorageTypeAndFilename()
    {
        var storage = CreateStorage();

        var first = storage.determineLocation("guide.pdf", new StorageOptions
        {
            FileOwnership = FileOwnership.Shared,
            StorageType = StorageType.Resources
        });
        var second = storage.determineLocation("policy.docx", new StorageOptions
        {
            FileOwnership = FileOwnership.Shared,
            StorageType = StorageType.Resources
        });

        first.Should().Be("_shared/resources/guide.pdf");
        second.Should().Be("_shared/resources/policy.docx");
        first.Should().NotBe(second);
    }

    [Fact]
    public void DetermineLocation_SharedPrefix_DoesNotCollapseToSingleBlob()
    {
        var storage = CreateStorage();

        var prefix = storage.determineLocation(string.Empty, new StorageOptions
        {
            FileOwnership = FileOwnership.Shared,
            StorageType = StorageType.Resources
        });

        prefix.Should().Be("_shared/resources");
        prefix.Should().NotBe(Storage.SHARED_FOLDER);
    }

    [Fact]
    public void DetermineLocation_UserFiles_KeepUserScopedPath()
    {
        var storage = CreateStorage();

        var location = storage.determineLocation("notes.md", new StorageOptions
        {
            UserId = "auth0|user-1",
            FileOwnership = FileOwnership.User,
            StorageType = StorageType.Resources
        });

        location.Should().Be("auth0|user-1/resources/notes.md");
    }

    [Fact]
    public void DetermineLocation_UserWithoutUserId_Throws()
    {
        var storage = CreateStorage();

        var act = () => storage.determineLocation("notes.md", new StorageOptions
        {
            FileOwnership = FileOwnership.User,
            StorageType = StorageType.Resources
        });

        act.Should().Throw<Exception>().WithMessage("User ID is required for user files");
    }
}
