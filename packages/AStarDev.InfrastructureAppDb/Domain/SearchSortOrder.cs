namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Controls the ordering of results returned by <see cref="SyncedItemSearchCriteria"/>.</summary>
public enum SearchSortOrder
{
    /// <summary>Order by filename A → Z.</summary>
    NameAscending,

    /// <summary>Order by filename Z → A.</summary>
    NameDescending,

    /// <summary>Order by file size smallest first.</summary>
    SizeAscending,

    /// <summary>Order by file size largest first.</summary>
    SizeDescending
}
