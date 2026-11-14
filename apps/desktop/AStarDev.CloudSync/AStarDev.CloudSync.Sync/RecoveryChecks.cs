namespace AStarDev.CloudSync.Sync;

/// <summary>What startup recovery asks before it touches an interrupted run (#1616, #1612).</summary>
/// <param name="paused">Tells which accounts the user paused; their interrupted runs are marked but not resumed.</param>
/// <param name="live">The runs this session is working on; recovery leaves them alone.</param>
/// <param name="health">Tells whether the database passed its integrity check; a failed check leaves the interrupted runs alone.</param>
internal sealed class RecoveryChecks(IAccountPauseReader paused, IDatabaseHealthGate health, LiveSyncRuns live)
{
    /// <summary>The paused accounts.</summary>
    public IAccountPauseReader Paused { get; } = paused;

    /// <summary>The runs in flight in this session.</summary>
    public LiveSyncRuns Live { get; } = live;

    /// <summary>The database health check.</summary>
    public IDatabaseHealthGate Health { get; } = health;
}
