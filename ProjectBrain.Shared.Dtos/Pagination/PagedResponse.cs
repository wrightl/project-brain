namespace ProjectBrain.Shared.Dtos.Pagination;

/// <summary>
/// Standard paginated response wrapper
/// </summary>
/// <typeparam name="T">The type of items in the response</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// The items for the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates a paged response from the request and results
    /// </summary>
    public static PagedResponse<T> Create(PagedRequest request, IEnumerable<T> items, int totalCount)
    {
        return new PagedResponse<T>
        {
            Items = items,
            Page = request.GetNormalizedPage(),
            PageSize = request.GetNormalizedPageSize(),
            TotalCount = totalCount
        };
    }
}

