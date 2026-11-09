using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Accounts;

/// <summary>The provider's identifier for the drive that belongs to an account.</summary>
[StrongType(typeof(string))]
public partial record struct DriveId
{
    /// <summary>Returns a redacted form, so the value never reaches logs by accident.</summary>
    public override string ToString() => "DriveId(redacted)";
}
