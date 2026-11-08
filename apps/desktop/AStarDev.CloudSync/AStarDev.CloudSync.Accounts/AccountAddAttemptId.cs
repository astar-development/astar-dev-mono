using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Accounts;

/// <summary>Identifies one add-account attempt in the logs, so every event of the attempt can be found together.</summary>
[StrongType]
public partial record struct AccountAddAttemptId;
