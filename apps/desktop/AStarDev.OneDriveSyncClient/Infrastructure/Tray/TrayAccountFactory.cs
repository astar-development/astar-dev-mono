namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <summary>Creates <see cref="TrayAccount"/> instances.</summary>
public static class TrayAccountFactory
{
    /// <summary>Creates a tray account, normalising a missing display name to an empty string.</summary>
    public static TrayAccount Create(string id, string? displayName) => new(id, displayName ?? string.Empty);
}
