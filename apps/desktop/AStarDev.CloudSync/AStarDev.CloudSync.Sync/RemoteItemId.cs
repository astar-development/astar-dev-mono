using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>The provider's identifier for a file or folder.</summary>
[StrongType(typeof(string))]
public partial record struct RemoteItemId;
