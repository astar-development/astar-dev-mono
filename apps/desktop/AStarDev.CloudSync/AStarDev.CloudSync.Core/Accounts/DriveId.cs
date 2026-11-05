using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Core.Accounts;

/// <summary>The provider's identifier for the drive that belongs to an account.</summary>
[StrongType(typeof(string))]
public partial record struct DriveId;
