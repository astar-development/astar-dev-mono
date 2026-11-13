using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>Identifies one resync pass of an account's remote index; items seen in the pass carry it, so unseen items are the ones that carry another value (#1459).</summary>
[StrongType(typeof(Guid))]
public partial record struct ResyncPassId;
