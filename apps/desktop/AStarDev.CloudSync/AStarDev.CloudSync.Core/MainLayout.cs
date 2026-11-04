namespace AStarDev.CloudSync.Core;

/// <summary>The main window layout the user has chosen.</summary>
public abstract record MainLayout
{
    private MainLayout()
    {
    }

    /// <summary>Layout A, the default: one list of every account.</summary>
    public sealed record UnifiedAccounts : MainLayout;

    /// <summary>Layout B: accounts grouped under their provider.</summary>
    public sealed record GroupedByProvider : MainLayout;

    /// <summary>Layout C: the window is organised around sync pairs.</summary>
    public sealed record SyncPairsFirst : MainLayout;
}
