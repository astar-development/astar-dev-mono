using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The outcome of one remote index update (#1398). Deletions are counted, so a deletion-only update never reads as "no changes".</summary>
public sealed record RemoteUpdateSummary
{
    internal RemoteUpdateSummary(RelevantChangeCounts changes, RuleRelocationSummary rules, Option<ResyncKind> resync, int pagesRead)
    {
        Changes = changes;
        Rules = rules;
        Resync = resync;
        PagesRead = pagesRead;
    }

    /// <summary>The relevant changes.</summary>
    public RelevantChangeCounts Changes { get; }

    /// <summary>What happened to the selection rules.</summary>
    public RuleRelocationSummary Rules { get; }

    /// <summary>The kind of resync that ran, none when the update was incremental.</summary>
    public Option<ResyncKind> Resync { get; }

    /// <summary>How many change pages were read.</summary>
    public int PagesRead { get; }
}
