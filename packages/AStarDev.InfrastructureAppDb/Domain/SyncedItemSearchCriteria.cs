using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Criteria for searching synced items in the repository.</summary>
public sealed record SyncedItemSearchCriteria(AccountId AccountId, string? NameFragment, long? MinBytes, long? MaxBytes, IReadOnlyList<string> Tags, bool DuplicatesOnly, SearchSortOrder SortOrder);
