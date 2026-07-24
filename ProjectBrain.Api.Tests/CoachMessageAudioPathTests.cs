using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using Azure.Storage.Blobs;

namespace ProjectBrain.Api.Tests;

/// <summary>
/// Coach voice blobs are uploaded under the sender's folder. Playback must resolve
/// that same path for both sender and recipient.
/// </summary>
public class CoachMessageAudioPathTests
{
    private static Storage CreateStorage()
    {
        return new Storage(
            new ConfigurationBuilder().Build(),
            new Mock<BlobServiceClient>().Object,
            new Mock<ILogger<Storage>>().Object,
            new Mock<ISearchIndexService>().Object);
    }

    [Fact]
    public void DetermineLocation_UsesUserIdAsBlobRoot_SoRecipientLookupMustUseSenderId()
    {
        var storage = CreateStorage();
        const string senderId = "auth0|sender";
        const string recipientId = "auth0|recipient";
        const string fileName = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee.m4a";

        var uploadPath = storage.determineLocation(
            fileName,
            new StorageOptions
            {
                UserId = senderId,
                StorageType = StorageType.CoachMessages,
                FileOwnership = FileOwnership.User,
                ParentFolder = senderId
            });

        var wrongRecipientLookup = storage.determineLocation(
            fileName,
            new StorageOptions
            {
                UserId = recipientId,
                StorageType = StorageType.CoachMessages,
                FileOwnership = FileOwnership.User,
                ParentFolder = senderId
            });

        var correctLookup = storage.determineLocation(
            fileName,
            new StorageOptions
            {
                UserId = senderId,
                StorageType = StorageType.CoachMessages,
                FileOwnership = FileOwnership.User,
                ParentFolder = senderId
            });

        uploadPath.Should().Be($"{senderId}/coach-messages/{senderId}/{fileName}");
        wrongRecipientLookup.Should().NotBe(uploadPath);
        correctLookup.Should().Be(uploadPath);
    }
}
