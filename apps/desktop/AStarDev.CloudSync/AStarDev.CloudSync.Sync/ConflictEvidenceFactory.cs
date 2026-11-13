using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ConflictEvidence"/> values.</summary>
public static class ConflictEvidenceFactory
{
    /// <summary>Creates the evidence for a path with no extra planner fact.</summary>
    /// <param name="local">What the local side holds now.</param>
    /// <param name="remote">What the cloud holds now.</param>
    /// <param name="baseline">The last agreed state, or none.</param>
    public static ConflictEvidence Create(SideSnapshot local, SideSnapshot remote, Option<ConflictBaseline> baseline) => new(local, remote, baseline, ConflictSignalFactory.CreateNone());

    /// <summary>Creates the evidence for a path with a planner fact.</summary>
    /// <param name="local">What the local side holds now.</param>
    /// <param name="remote">What the cloud holds now.</param>
    /// <param name="baseline">The last agreed state, or none.</param>
    /// <param name="signal">The planner's fact.</param>
    public static ConflictEvidence Create(SideSnapshot local, SideSnapshot remote, Option<ConflictBaseline> baseline, ConflictSignal signal) => new(local, remote, baseline, signal);
}
