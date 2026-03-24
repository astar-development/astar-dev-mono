using AStar.Dev.Technical.Debt.Reporting;

namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     The currently supported SortOrders
/// </summary>
[Refactor(1, 1, "I think this is now duplicated too")]
public enum SortOrder
{
    /// <summary>
    ///     Order by the size descending
    /// </summary>
    SizeDescending,

    /// <summary>
    ///     Order by the size ascending
    /// </summary>
    SizeAscending,

    /// <summary>
    ///     Order by the name descending
    /// </summary>
    NameDescending,

    /// <summary>
    ///     Order by the name ascending
    /// </summary>
    NameAscending
}