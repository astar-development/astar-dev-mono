using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>Identifies a sync pair: one local folder linked to one cloud folder.</summary>
[StrongType]
public partial record struct SyncPairId;
