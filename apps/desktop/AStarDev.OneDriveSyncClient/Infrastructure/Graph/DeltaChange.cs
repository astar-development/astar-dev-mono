using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Graph;

/// <summary>A single change reported by the Graph delta endpoint.</summary>
/// <param name="ItemId">The OneDrive item id the change applies to.</param>
public abstract record DeltaChange(string ItemId);

/// <summary>An item that was created, modified, renamed or moved.</summary>
/// <param name="ItemId">The OneDrive item id.</param>
/// <param name="ParentId">The id of the item's parent folder, when Graph supplied it.</param>
public sealed record ItemChanged(string ItemId, Option<string> ParentId) : DeltaChange(ItemId);

/// <summary>An item that was deleted.</summary>
/// <param name="ItemId">The OneDrive item id.</param>
public sealed record ItemDeleted(string ItemId) : DeltaChange(ItemId);
