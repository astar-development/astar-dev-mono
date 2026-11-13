using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Everything <see cref="IConflictClassifier"/> needs to judge one path.</summary>
public sealed record ConflictEvidence
{
    internal ConflictEvidence(SideSnapshot local, SideSnapshot remote, Option<ConflictBaseline> baseline, ConflictSignal signal)
    {
        Local = local;
        Remote = remote;
        Baseline = baseline;
        Signal = signal;
    }

    /// <summary>What the local side holds now.</summary>
    public SideSnapshot Local { get; }

    /// <summary>What the cloud holds now.</summary>
    public SideSnapshot Remote { get; }

    /// <summary>What both sides agreed on at the last sync; none when the path was never synced.</summary>
    public Option<ConflictBaseline> Baseline { get; }

    /// <summary>A fact the planner knows beyond the snapshots.</summary>
    public ConflictSignal Signal { get; }
}
