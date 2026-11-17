using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>The last reclassify run that completed: the engine and seed versions it ran with and when it finished.</summary>
public sealed record ReclassifyRunRecord
{
    internal ReclassifyRunRecord(string engineVersion, Option<string> seedVersion, DateTimeOffset finishedAt)
    {
        EngineVersion = engineVersion;
        SeedVersion = seedVersion;
        FinishedAt = finishedAt;
    }

    /// <summary>The <see cref="ClassificationEngineVersion"/> the run used.</summary>
    public string EngineVersion { get; }

    /// <summary>The seed version applied when the run started, or none when no seed was applied yet.</summary>
    public Option<string> SeedVersion { get; }

    /// <summary>When the run finished.</summary>
    public DateTimeOffset FinishedAt { get; }
}
