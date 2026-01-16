namespace Enterprise.Application.Common.Models;

/// <summary>
/// Cursor-based pagination result for scalable pagination without OFFSET/SKIP performance issues.
/// Ideal for large datasets (millions of records) and infinite scroll UIs.
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class CursorPaginatedResult<T>
{
    /// <summary>
    /// The items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Cursor pointing to the next page (typically the ID of the last item)
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Cursor pointing to the previous page (typically the ID of the first item)
    /// </summary>
    public string? PreviousCursor { get; set; }

    /// <summary>
    /// Number of items in the current page
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Whether there are more items available
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there are previous items available
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// The page size used for this request
    /// </summary>
    public int PageSize { get; set; }

    public CursorPaginatedResult()
    {
    }

    public CursorPaginatedResult(IEnumerable<T> items, string? nextCursor, string? previousCursor, int pageSize)
    {
        Items = items;
        Count = items.Count();
        NextCursor = nextCursor;
        PreviousCursor = previousCursor;
        PageSize = pageSize;
        HasNextPage = !string.IsNullOrEmpty(nextCursor);
        HasPreviousPage = !string.IsNullOrEmpty(previousCursor);
    }

    /// <summary>
    /// Creates an empty result
    /// </summary>
    public static CursorPaginatedResult<T> Empty(int pageSize = 20)
    {
        return new CursorPaginatedResult<T>
        {
            Items = new List<T>(),
            Count = 0,
            NextCursor = null,
            PreviousCursor = null,
            HasNextPage = false,
            HasPreviousPage = false,
            PageSize = pageSize
        };
    }
}
