using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>Identifies one set of deletions held by the mass-deletion guard in the attention queue (#1405).</summary>
[StrongType(typeof(Guid))]
public partial record struct HeldDeletionId;
