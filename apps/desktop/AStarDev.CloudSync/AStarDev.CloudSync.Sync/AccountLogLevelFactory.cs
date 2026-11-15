namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="AccountLogLevel"/> values.</summary>
public static class AccountLogLevelFactory
{
    /// <summary>Creates the <see cref="AccountLogLevel.Standard"/> case.</summary>
    public static AccountLogLevel CreateStandard() => new AccountLogLevel.Standard();

    /// <summary>Creates the <see cref="AccountLogLevel.Verbose"/> case.</summary>
    public static AccountLogLevel CreateVerbose() => new AccountLogLevel.Verbose();
}
