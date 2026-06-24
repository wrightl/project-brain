namespace ProjectBrain.Domain.Dtos;

public sealed class ChatCitationDto
{
    public string Id { get; init; } = string.Empty;
    public int Index { get; init; }
    public string SourceFile { get; init; } = string.Empty;
    public string SourcePage { get; init; } = string.Empty;
    public string StorageUrl { get; init; } = string.Empty;
    public bool IsShared { get; init; }
}

public sealed class ChatRetrievalResult
{
    public IReadOnlyList<ChatCitationDto> Citations { get; init; } = Array.Empty<ChatCitationDto>();
    public string SourcesFormatted { get; init; } = string.Empty;
}
