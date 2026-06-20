namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>Captures the outcome of a single sync pass for one account.</summary>
public sealed record SyncPassResult(bool DidRun, int FailedJobCount);
