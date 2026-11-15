namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Creates <see cref="TrayNoticeState"/> values.</summary>
public static class TrayNoticeStateFactory
{
    /// <summary>Creates the <see cref="TrayNoticeState.Pending"/> case.</summary>
    public static TrayNoticeState CreatePending() => new TrayNoticeState.Pending();

    /// <summary>Creates the <see cref="TrayNoticeState.Shown"/> case.</summary>
    public static TrayNoticeState CreateShown() => new TrayNoticeState.Shown();
}
