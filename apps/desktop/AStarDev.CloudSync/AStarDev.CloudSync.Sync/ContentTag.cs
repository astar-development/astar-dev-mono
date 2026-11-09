using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>The provider's content tag (OneDrive cTag); it changes only when the content changes.</summary>
[StrongType(typeof(string))]
public partial record struct ContentTag;
