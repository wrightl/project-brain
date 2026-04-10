using FluentAssertions;
using System.Reflection;

namespace ProjectBrain.Api.Tests;

public class VoiceNotesEndpointTests
{
    [Fact]
    public void ResolveStoredBlobFileName_ShouldUseFileNameFromFilePath_WhenPresent()
    {
        var voiceNote = new VoiceNote
        {
            Id = Guid.NewGuid(),
            UserId = "auth0|voice",
            FileName = "original-upload-name.m4a",
            FilePath = "auth0|voice/voice-notes/6f8f2d0e-5ac9-4fc6-8468-53dc17d2f088.m4a",
            AudioUrl = "https://example.com/voicenotes/6f8f2d0e-5ac9-4fc6-8468-53dc17d2f088/audio"
        };

        var method = typeof(VoiceNoteEndpoints).GetMethod(
            "ResolveStoredBlobFileName",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = (string)method!.Invoke(null, new object[] { voiceNote })!;

        result.Should().Be("6f8f2d0e-5ac9-4fc6-8468-53dc17d2f088.m4a");
    }

    [Fact]
    public void ResolveStoredBlobFileName_ShouldFallbackToFileName_WhenFilePathMissing()
    {
        var voiceNote = new VoiceNote
        {
            Id = Guid.NewGuid(),
            UserId = "auth0|voice",
            FileName = "fallback-name.m4a",
            FilePath = string.Empty,
            AudioUrl = "https://example.com/voicenotes/fallback/audio"
        };

        var method = typeof(VoiceNoteEndpoints).GetMethod(
            "ResolveStoredBlobFileName",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = (string)method!.Invoke(null, new object[] { voiceNote })!;

        result.Should().Be("fallback-name.m4a");
    }
}
