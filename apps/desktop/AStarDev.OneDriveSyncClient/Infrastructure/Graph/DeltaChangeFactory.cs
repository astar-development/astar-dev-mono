using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Graph;

/// <summary>Creates <see cref="DeltaChange"/> cases.</summary>
public static class DeltaChangeFactory
{
    /// <summary>Creates an <see cref="ItemChanged"/> case.</summary>
    public static DeltaChange CreateChanged(string itemId, Option<string> parentId) => new ItemChanged(itemId, parentId);

    /// <summary>Creates an <see cref="ItemDeleted"/> case.</summary>
    public static DeltaChange CreateDeleted(string itemId) => new ItemDeleted(itemId);
}
