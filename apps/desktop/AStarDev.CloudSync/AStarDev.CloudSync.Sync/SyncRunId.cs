using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>Identifies one sync run in the run journal (#1610).</summary>
[StrongType(typeof(Guid))]
public partial record struct SyncRunId;
