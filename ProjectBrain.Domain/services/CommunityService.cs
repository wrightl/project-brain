namespace ProjectBrain.Domain;

using System.Net;
using System.Text.RegularExpressions;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;
using ProjectBrain.Shared.Dtos.Community;

public interface ICommunityService
{
    Task EnsureChannelsSeededAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommunityChannelDto>> GetChannelsAsync(CancellationToken cancellationToken = default);
    Task<CommunityPostsPageDto> GetPostsAsync(string channelSlug, string? cursor, int limit, string currentUserId, CancellationToken cancellationToken = default);
    Task<CommunityPostsPageDto> GetLatestFeedAsync(string? cursor, int limit, string currentUserId, CancellationToken cancellationToken = default);
    Task<CommunityPostDto> CreatePostAsync(string channelSlug, string authorUserId, string body, CancellationToken cancellationToken = default);
    Task DeleteOwnPostAsync(Guid postId, string userId, CancellationToken cancellationToken = default);
    Task AddReactionAsync(Guid postId, string userId, CancellationToken cancellationToken = default);
    Task RemoveReactionAsync(Guid postId, string userId, CancellationToken cancellationToken = default);
    Task CreateReportAsync(Guid postId, string reporterUserId, string reason, CancellationToken cancellationToken = default);
    Task HidePostAsync(Guid postId, string adminUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommunityReportDto>> GetOpenReportsAsync(CancellationToken cancellationToken = default);
    Task ResolveReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<CommunityChannelDto> UpdateChannelAsync(Guid channelId, UpdateCommunityChannelRequestDto request, CancellationToken cancellationToken = default);
}

public class CommunityService : ICommunityService
{
    private const int MaxBodyLength = 2000;
    private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly ICommunityChannelRepository _channelRepository;
    private readonly ICommunityPostRepository _postRepository;
    private readonly ICommunityReactionRepository _reactionRepository;
    private readonly ICommunityReportRepository _reportRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CommunityService(
        ICommunityChannelRepository channelRepository,
        ICommunityPostRepository postRepository,
        ICommunityReactionRepository reactionRepository,
        ICommunityReportRepository reportRepository,
        IUnitOfWork unitOfWork)
    {
        _channelRepository = channelRepository;
        _postRepository = postRepository;
        _reactionRepository = reactionRepository;
        _reportRepository = reportRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task EnsureChannelsSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await _channelRepository.CountAsync(cancellationToken) > 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var seeds = new[]
        {
            new CommunityChannel { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Slug = "introductions", Name = "Introductions", Description = "Say hello and share a bit about yourself.", SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new CommunityChannel { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Slug = "wins", Name = "Wins", Description = "Celebrate small and big wins.", SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new CommunityChannel { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Slug = "struggles", Name = "Struggles", Description = "Share what's hard — you're not alone.", SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new CommunityChannel { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Slug = "tips", Name = "Tips", Description = "Practical tips and strategies that help.", SortOrder = 4, IsActive = true, CreatedAt = now, UpdatedAt = now },
        };

        _channelRepository.AddRange(seeds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommunityChannelDto>> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureChannelsSeededAsync(cancellationToken);
        var channels = await _channelRepository.GetActiveOrderedAsync(cancellationToken);
        return channels.Select(ToChannelDto).ToList();
    }

    public async Task<CommunityPostsPageDto> GetPostsAsync(
        string channelSlug,
        string? cursor,
        int limit,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var channel = await GetActiveChannelBySlugAsync(channelSlug, cancellationToken);
        ParseCursor(cursor, out var cursorCreatedAt, out var cursorId);

        var (items, hasMore) = await _postRepository.GetPagedByChannelAsync(
            channel.Id,
            cursorCreatedAt,
            cursorId,
            limit,
            cancellationToken);

        var postIds = items.Select(p => p.Id).ToList();
        var counts = await _reactionRepository.CountByPostIdsAsync(postIds, cancellationToken);
        var reacted = await _reactionRepository.GetReactedPostIdsAsync(currentUserId, postIds, cancellationToken);

        var dtos = items.Select(p => ToPostDto(
            p,
            currentUserId,
            counts.GetValueOrDefault(p.Id),
            reacted.Contains(p.Id))).ToList();

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var last = items[^1];
            nextCursor = EncodeCursor(last.CreatedAt, last.Id);
        }

        return new CommunityPostsPageDto { Items = dtos, NextCursor = nextCursor };
    }

    public async Task<CommunityPostsPageDto> GetLatestFeedAsync(
        string? cursor,
        int limit,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureChannelsSeededAsync(cancellationToken);
        ParseCursor(cursor, out var cursorCreatedAt, out var cursorId);

        var (items, hasMore) = await _postRepository.GetPagedLatestAsync(
            cursorCreatedAt,
            cursorId,
            limit,
            cancellationToken);

        var postIds = items.Select(p => p.Id).ToList();
        var counts = await _reactionRepository.CountByPostIdsAsync(postIds, cancellationToken);
        var reacted = await _reactionRepository.GetReactedPostIdsAsync(currentUserId, postIds, cancellationToken);

        var dtos = items.Select(p => ToPostDto(
            p,
            currentUserId,
            counts.GetValueOrDefault(p.Id),
            reacted.Contains(p.Id))).ToList();

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var last = items[^1];
            nextCursor = EncodeCursor(last.CreatedAt, last.Id);
        }

        return new CommunityPostsPageDto { Items = dtos, NextCursor = nextCursor };
    }

    public async Task<CommunityPostDto> CreatePostAsync(
        string channelSlug,
        string authorUserId,
        string body,
        CancellationToken cancellationToken = default)
    {
        var channel = await GetActiveChannelBySlugAsync(channelSlug, cancellationToken);
        var cleaned = SanitizeBody(body);

        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            ChannelId = channel.Id,
            AuthorUserId = authorUserId,
            Body = cleaned,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _postRepository.Add(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var loaded = await _postRepository.GetByIdWithAuthorAsync(post.Id, cancellationToken)
            ?? throw new AppException("POST_CREATE_FAILED", "Failed to load created post", 500);

        return ToPostDto(loaded, authorUserId, 0, false);
    }

    public async Task DeleteOwnPostAsync(Guid postId, string userId, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new AppException("NOT_FOUND", "Post not found", 404);

        if (post.DeletedAt != null || post.IsHidden)
        {
            throw new AppException("NOT_FOUND", "Post not found", 404);
        }

        if (!string.Equals(post.AuthorUserId, userId, StringComparison.Ordinal))
        {
            throw new AppException("FORBIDDEN", "You can only delete your own posts", 403);
        }

        post.DeletedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        _postRepository.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddReactionAsync(Guid postId, string userId, CancellationToken cancellationToken = default)
    {
        await EnsureVisiblePostAsync(postId, cancellationToken);

        var existing = await _reactionRepository.GetByPostAndUserAsync(postId, userId, cancellationToken);
        if (existing != null)
        {
            return;
        }

        _reactionRepository.Add(new CommunityReaction
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveReactionAsync(Guid postId, string userId, CancellationToken cancellationToken = default)
    {
        var existing = await _reactionRepository.GetByPostAndUserAsync(postId, userId, cancellationToken);
        if (existing == null)
        {
            return;
        }

        _reactionRepository.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateReportAsync(Guid postId, string reporterUserId, string reason, CancellationToken cancellationToken = default)
    {
        await EnsureVisiblePostAsync(postId, cancellationToken);
        var cleanedReason = SanitizeBody(reason, maxLength: 500);

        _reportRepository.Add(new CommunityReport
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            ReporterUserId = reporterUserId,
            Reason = cleanedReason,
            Status = CommunityReport.StatusOpen,
            CreatedAt = DateTime.UtcNow,
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task HidePostAsync(Guid postId, string adminUserId, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new AppException("NOT_FOUND", "Post not found", 404);

        post.IsHidden = true;
        post.HiddenByUserId = adminUserId;
        post.HiddenAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        _postRepository.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommunityReportDto>> GetOpenReportsAsync(CancellationToken cancellationToken = default)
    {
        var reports = await _reportRepository.GetOpenWithDetailsAsync(cancellationToken);
        return reports.Select(r => new CommunityReportDto
        {
            Id = r.Id,
            PostId = r.PostId,
            PostBodyPreview = Truncate(r.Post?.Body ?? string.Empty, 120),
            ChannelSlug = r.Post?.Channel?.Slug ?? string.Empty,
            ReporterUserId = r.ReporterUserId,
            ReporterDisplayName = r.Reporter?.FullName ?? "Unknown",
            Reason = r.Reason,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
        }).ToList();
    }

    public async Task ResolveReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken)
            ?? throw new AppException("NOT_FOUND", "Report not found", 404);

        report.Status = CommunityReport.StatusResolved;
        _reportRepository.Update(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CommunityChannelDto> UpdateChannelAsync(
        Guid channelId,
        UpdateCommunityChannelRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken)
            ?? throw new AppException("NOT_FOUND", "Channel not found", 404);

        if (request.IsActive.HasValue)
        {
            channel.IsActive = request.IsActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            channel.Name = request.Name.Trim();
        }

        if (request.Description != null)
        {
            channel.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
        }

        if (request.SortOrder.HasValue)
        {
            channel.SortOrder = request.SortOrder.Value;
        }

        channel.UpdatedAt = DateTime.UtcNow;
        _channelRepository.Update(channel);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToChannelDto(channel);
    }

    private async Task<CommunityChannel> GetActiveChannelBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        await EnsureChannelsSeededAsync(cancellationToken);
        var channel = await _channelRepository.GetBySlugAsync(slug, cancellationToken);
        if (channel == null || !channel.IsActive)
        {
            throw new AppException("NOT_FOUND", "Channel not found", 404);
        }

        return channel;
    }

    private async Task EnsureVisiblePostAsync(Guid postId, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null || post.DeletedAt != null || post.IsHidden)
        {
            throw new AppException("NOT_FOUND", "Post not found", 404);
        }
    }

    private static string SanitizeBody(string body, int maxLength = MaxBodyLength)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new AppException("VALIDATION", "Body is required", 400);
        }

        var decoded = WebUtility.HtmlDecode(body);
        var stripped = HtmlTagRegex.Replace(decoded, string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(stripped))
        {
            throw new AppException("VALIDATION", "Body is required", 400);
        }

        if (stripped.Length > maxLength)
        {
            throw new AppException("VALIDATION", $"Body must be at most {maxLength} characters", 400);
        }

        return stripped;
    }

    private static CommunityChannelDto ToChannelDto(CommunityChannel channel) => new()
    {
        Id = channel.Id,
        Slug = channel.Slug,
        Name = channel.Name,
        Description = channel.Description,
        SortOrder = channel.SortOrder,
    };

    private static CommunityPostDto ToPostDto(
        CommunityPost post,
        string currentUserId,
        int reactionCount,
        bool reactedByCurrentUser) => new()
    {
        Id = post.Id,
        ChannelId = post.ChannelId,
        ChannelSlug = post.Channel?.Slug ?? string.Empty,
        AuthorUserId = post.AuthorUserId,
        AuthorDisplayName = post.Author?.FullName ?? "Member",
        Body = post.Body,
        CreatedAt = post.CreatedAt,
        UpdatedAt = post.UpdatedAt,
        ReactionCount = reactionCount,
        ReactedByCurrentUser = reactedByCurrentUser,
        IsOwnPost = string.Equals(post.AuthorUserId, currentUserId, StringComparison.Ordinal),
    };

    private static void ParseCursor(string? cursor, out DateTime? createdAt, out Guid? id)
    {
        createdAt = null;
        id = null;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return;
        }

        var parts = cursor.Split('|', 2);
        if (parts.Length != 2)
        {
            throw new AppException("VALIDATION", "Invalid cursor", 400);
        }

        if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedAt) ||
            !Guid.TryParse(parts[1], out var parsedId))
        {
            throw new AppException("VALIDATION", "Invalid cursor", 400);
        }

        createdAt = parsedAt;
        id = parsedId;
    }

    private static string EncodeCursor(DateTime createdAt, Guid id) =>
        $"{createdAt:O}|{id}";

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
