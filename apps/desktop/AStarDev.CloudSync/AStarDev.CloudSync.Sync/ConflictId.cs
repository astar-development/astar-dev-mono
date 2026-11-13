using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>Identifies one conflict in the attention queue (#1405).</summary>
[StrongType(typeof(Guid))]
public partial record struct ConflictId;
