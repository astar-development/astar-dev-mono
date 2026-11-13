using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteUpdateSummary"/> values.</summary>
public static class RemoteUpdateSummaryFactory
{
    /// <summary>Creates the summary; a negative page count becomes zero.</summary>
    public static RemoteUpdateSummary Create(RelevantChangeCounts changes, RuleRelocationSummary rules, Option<ResyncKind> resync, int pagesRead) => new(changes, rules, resync, Math.Max(pagesRead, 0));
}
