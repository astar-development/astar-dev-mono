using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The state of one remote index update: the cursor, the resync, the links already seen and the running totals.</summary>
/// <param name="accountId">The account.</param>
/// <param name="cursor">The saved cursor, none on the first run.</param>
/// <param name="resync">The resync that was running when the update started, none when there was none.</param>
internal sealed class RemoteUpdateRun(AccountId accountId, Option<ChangeCursor> cursor, Option<ResyncKind> resync)
{
    private RelevantChangeCounts counts = RelevantChangeCountsFactory.None;
    private RuleRelocationSummary rules = RuleRelocationSummaryFactory.None;
    private int pages;

    internal AccountId AccountId { get; } = accountId;

    internal Option<ChangeCursor> Cursor { get; set; } = cursor;

    internal Option<ResyncKind> Resync { get; set; } = resync;

    internal int Expirations { get; set; }

    internal HashSet<string> Seen { get; } = cursor.Match(found => new HashSet<string>(StringComparer.Ordinal) { found.Value }, () => new HashSet<string>(StringComparer.Ordinal));

    internal void Add(RelevantChangeCounts found, RuleRelocationSummary relocated)
    {
        counts = counts.Plus(found);
        rules = RuleRelocationSummaryFactory.Create(rules.Relocated + relocated.Relocated, rules.Dropped + relocated.Dropped);
    }

    internal void CountPage() => pages++;

    internal RemoteUpdateSummary ToSummary() => RemoteUpdateSummaryFactory.Create(counts, rules, Resync, pages);
}
