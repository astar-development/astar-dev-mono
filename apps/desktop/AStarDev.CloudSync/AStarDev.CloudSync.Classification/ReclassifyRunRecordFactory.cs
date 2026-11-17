using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ReclassifyRunRecord"/> values.</summary>
public static class ReclassifyRunRecordFactory
{
    /// <summary>Creates the record. A missing engine version is stored as empty, which never matches the current one.</summary>
    /// <param name="engineVersion">The engine version the run used.</param>
    /// <param name="seedVersion">The seed version applied when the run started.</param>
    /// <param name="finishedAt">When the run finished.</param>
    public static ReclassifyRunRecord Create(string? engineVersion, Option<string> seedVersion, DateTimeOffset finishedAt) => new(engineVersion ?? string.Empty, seedVersion, finishedAt);
}
