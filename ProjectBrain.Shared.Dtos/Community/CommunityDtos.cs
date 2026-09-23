namespace ProjectBrain.Shared.Dtos.Community;

public class CommunityChannelDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class CommunityPostDto
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public string ChannelSlug { get; set; } = string.Empty;
    public string AuthorUserId { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ReactionCount { get; set; }
    public bool ReactedByCurrentUser { get; set; }
    public bool IsOwnPost { get; set; }
}

public class CommunityPostsPageDto
{
    public IReadOnlyList<CommunityPostDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public class CreateCommunityPostRequestDto
{
    public string Body { get; set; } = string.Empty;
}

public class CreateCommunityReportRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public class CommunityReportDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string PostBodyPreview { get; set; } = string.Empty;
    public string ChannelSlug { get; set; } = string.Empty;
    public string ReporterUserId { get; set; } = string.Empty;
    public string ReporterDisplayName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateCommunityChannelRequestDto
{
    public bool? IsActive { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}
