using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>An opaque position in a provider's change feed; OneDrive stores the delta link, which carries a token, so it is redacted.</summary>
[StrongType(typeof(string))]
public partial record struct ChangeCursor
{
    /// <summary>Returns a redacted form, so the value never reaches logs by accident.</summary>
    public override string ToString() => "ChangeCursor(redacted)";
}
