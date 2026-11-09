using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>The provider's content hash (OneDrive QuickXorHash or SHA-256).</summary>
[StrongType(typeof(string))]
public partial record struct ContentHash;
