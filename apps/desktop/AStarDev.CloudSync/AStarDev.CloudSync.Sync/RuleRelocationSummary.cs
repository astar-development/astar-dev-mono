namespace AStarDev.CloudSync.Sync;

/// <summary>What relocating selection rules after remote moves did (#1398).</summary>
public sealed record RuleRelocationSummary
{
    internal RuleRelocationSummary(int relocated, int dropped)
    {
        Relocated = relocated;
        Dropped = dropped;
    }

    /// <summary>Rules that followed their folder to its new path.</summary>
    public int Relocated { get; }

    /// <summary>Rules dropped because the new path already had a rule, which wins.</summary>
    public int Dropped { get; }
}
