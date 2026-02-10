namespace MyOrg.CurrencyConverter.API.Core.Models;

/// <summary>
/// Contains pagination metadata for paginated responses
/// </summary>
public class PaginationMetadata
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage { get; set; }

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
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates whether there is a next page
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;

    /// <summary>
    /// Indicates whether there is a previous page
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;
}
