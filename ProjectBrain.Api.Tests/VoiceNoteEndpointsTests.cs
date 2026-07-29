using FluentAssertions;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Tests;

public class VoiceNoteEndpointsTests
{
    [Fact]
    public void ResolveStorageFileName_ShouldPreferBlobNameFromFilePath()
    {
        // Simulates current production rows where FileName is user-facing
        // while FilePath stores the actual blob key used by Storage.
        var voiceNote = new VoiceNote
        {
            Id = Guid.NewGuid(),
            UserId = "auth0|user",
            FileName = "voice-note-1712345678.webm",
            FilePath = "auth0|user/voice-notes/3f530da6-ffde-44ad-bf43-1f7dd09a26bb.webm",
            AudioUrl = "https://api.example.com/voicenotes/audio",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var method = typeof(VoiceNoteEndpoints).GetMethod(
            "ResolveStorageFileName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var resolved = (string)method!.Invoke(null, new object[] { voiceNote })!;

        resolved.Should().Be("3f530da6-ffde-44ad-bf43-1f7dd09a26bb.webm");
    }

    [Fact]
    public void ResolveStorageFileName_ShouldFallbackToFileNameWhenPathMissing()
    {
        var voiceNote = new VoiceNote
        {
            Id = Guid.NewGuid(),
            UserId = "auth0|user",
            FileName = "voice-note-fallback.m4a",
            FilePath = string.Empty,
            AudioUrl = "https://api.example.com/voicenotes/audio",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var method = typeof(VoiceNoteEndpoints).GetMethod(
            "ResolveStorageFileName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var resolved = (string)method!.Invoke(null, new object[] { voiceNote })!;

        resolved.Should().Be("voice-note-fallback.m4a");
    }
}
