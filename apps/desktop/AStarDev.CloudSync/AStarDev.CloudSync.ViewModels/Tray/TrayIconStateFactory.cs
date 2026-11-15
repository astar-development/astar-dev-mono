namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Creates <see cref="TrayIconState"/> values.</summary>
public static class TrayIconStateFactory
{
    /// <summary>Creates the <see cref="TrayIconState.Idle"/> case.</summary>
    public static TrayIconState CreateIdle() => new TrayIconState.Idle();

    /// <summary>Creates the <see cref="TrayIconState.Syncing"/> case.</summary>
    public static TrayIconState CreateSyncing() => new TrayIconState.Syncing();

    /// <summary>Creates the <see cref="TrayIconState.Attention"/> case.</summary>
    public static TrayIconState CreateAttention() => new TrayIconState.Attention();

    /// <summary>Creates the <see cref="TrayIconState.Faulted"/> case.</summary>
    public static TrayIconState CreateFaulted() => new TrayIconState.Faulted();
}
