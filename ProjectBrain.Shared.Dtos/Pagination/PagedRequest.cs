namespace ProjectBrain.Shared.Dtos.Pagination;

/// <summary>
/// Standard pagination request parameters
/// </summary>
public class PagedRequest
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>
    /// Sort field name
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string? SortDirection { get; set; } = "desc";

    /// <summary>
    /// Gets the normalized page number (ensures >= 1)
    /// </summary>
    public int GetNormalizedPage() => Page < 1 ? 1 : Page;

    /// <summary>
    /// Gets the normalized page size (ensures within bounds)
    /// </summary>
    public int GetNormalizedPageSize() => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };

    /// <summary>
    /// Gets the skip count for database queries
    /// </summary>
    public int GetSkip() => (GetNormalizedPage() - 1) * GetNormalizedPageSize();

    /// <summary>
    /// Gets the take count for database queries
    /// </summary>
    public int GetTake() => GetNormalizedPageSize();
}

