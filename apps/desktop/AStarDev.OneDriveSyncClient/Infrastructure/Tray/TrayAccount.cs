namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <summary>The account details the tray menu needs to offer a per-account Sync Now entry.</summary>
/// <param name="Id">The raw account identifier passed to the sync scheduler.</param>
/// <param name="DisplayName">The name shown in the menu.</param>
public sealed record TrayAccount(string Id, string DisplayName);
