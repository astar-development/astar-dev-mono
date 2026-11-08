namespace AStarDev.CloudSync.Sync;

/// <summary>The one rule every planner applies before it plans a change.</summary>
public static class PathQuarantineStateExtensions
{
    /// <summary>Whether a pass may delete, repair-download or upload the path. True only for <see cref="PathQuarantineState.Clear"/>.</summary>
    /// <param name="state">The path's quarantine state.</param>
    public static bool AllowsSyncAction(this PathQuarantineState state) => throw new NotImplementedException($"{state}");
}
