namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>An executable and its arguments, passed without a shell.</summary>
/// <param name="FileName">The executable to run.</param>
/// <param name="Arguments">The arguments, each passed as a separate argument.</param>
public sealed record ProcessCommand(string FileName, IReadOnlyList<string> Arguments);
