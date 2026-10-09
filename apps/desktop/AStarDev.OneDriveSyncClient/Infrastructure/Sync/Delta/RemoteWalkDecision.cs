using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <summary>Whether the remote tree must be walked on this pass.</summary>
public abstract record RemoteWalkDecision;

/// <summary>The remote tree must be walked.</summary>
/// <param name="Reason">One of <see cref="WalkReasons"/>.</param>
/// <param name="NewDeltaLink">The delta link to store once the pass completes cleanly, when one could be obtained.</param>
public sealed record WalkRemote(string Reason, Option<string> NewDeltaLink) : RemoteWalkDecision;

/// <summary>Nothing relevant changed remotely, so the remote walk can be skipped.</summary>
/// <param name="NewDeltaLink">The delta link to store once the pass completes cleanly.</param>
public sealed record SkipRemote(string NewDeltaLink) : RemoteWalkDecision;
