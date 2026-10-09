using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <summary>Creates <see cref="RemoteWalkDecision"/> cases.</summary>
public static class RemoteWalkDecisionFactory
{
    /// <summary>Creates a <see cref="WalkRemote"/> case.</summary>
    public static RemoteWalkDecision CreateWalk(string reason, Option<string> newDeltaLink) => new WalkRemote(reason, newDeltaLink);

    /// <summary>Creates a <see cref="SkipRemote"/> case.</summary>
    public static RemoteWalkDecision CreateSkip(string newDeltaLink) => new SkipRemote(newDeltaLink);
}
