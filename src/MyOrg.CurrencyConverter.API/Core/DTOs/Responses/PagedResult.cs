namespace MyOrg.CurrencyConverter.API.Core.DTOs.Responses;

/// <summary>
/// Generic wrapper for paginated responses
/// </summary>
/// <typeparam name="T">The type of data being paginated</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The paginated data
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// Pagination metadata
    /// </summary>
    public PaginationMetadata Pagination { get; set; }

    public PagedResult(T data, PaginationMetadata pagination)
    {
        Data = data;
        Pagination = pagination;
    }

    /// <summary>
    /// Creates a PagedResult with automatically calculated pagination metadata
    /// </summary>
    /// <param name="data">The paginated data</param>
    /// <param name="currentPage">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="totalCount">Total number of items across all pages</param>
    /// <returns>A new PagedResult instance with calculated metadata</returns>
    public static PagedResult<T> Create(T data, int currentPage, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagination = new PaginationMetadata
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return new PagedResult<T>(data, pagination);
    }
}
