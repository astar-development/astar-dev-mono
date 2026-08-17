namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync;

/// <summary>Thrown when MSAL requires interactive re-authentication during a sync operation.</summary>
public sealed class SyncReAuthRequiredException() : Exception("Interactive re-authentication is required to continue syncing.");
