using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="SyncedItemSearchCriteria"/>.</summary>
public static class SyncedItemSearchCriteriaFactory
{
    /// <summary>Creates a <see cref="SyncedItemSearchCriteria"/> with the specified search parameters.</summary>
    public static SyncedItemSearchCriteria Create(AccountId accountId, string? nameFragment = null, long? minBytes = null, long? maxBytes = null, IReadOnlyList<string>? tags = null, bool duplicatesOnly = false, SearchSortOrder sortOrder = SearchSortOrder.NameAscending) => new(accountId, nameFragment, minBytes, maxBytes, tags ?? [], duplicatesOnly, sortOrder);
}
