using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>The provider's entity tag (OneDrive eTag); it changes on any change, metadata included.</summary>
[StrongType(typeof(string))]
public partial record struct EntityTag;
