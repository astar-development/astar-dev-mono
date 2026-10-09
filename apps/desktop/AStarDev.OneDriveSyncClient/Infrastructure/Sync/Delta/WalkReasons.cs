namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <summary>Reasons a full remote walk is required, used for planning and logging.</summary>
public static class WalkReasons
{
    /// <summary>No delta link has been stored yet, or the stored one has expired.</summary>
    public const string NoDeltaLink = "NoDeltaLink";

    /// <summary>The sync rules differ from those in force at the last full enumeration.</summary>
    public const string RulesChanged = "RulesChanged";

    /// <summary>No full enumeration has completed recently enough.</summary>
    public const string EnumerationTooOld = "EnumerationTooOld";

    /// <summary>An include rule has no resolved remote item id yet.</summary>
    public const string RuleWithoutItemId = "RuleWithoutItemId";

    /// <summary>The delta query reported relevant changes.</summary>
    public const string RelevantChanges = "RelevantChanges";

    /// <summary>Nothing relevant changed remotely since the stored delta link.</summary>
    public const string NoChanges = "NoChanges";

    /// <summary>The delta query failed or its link has expired.</summary>
    public const string DeltaUnavailable = "DeltaUnavailable";
}
