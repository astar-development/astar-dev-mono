namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RuleRelocationSummary"/> values.</summary>
public static class RuleRelocationSummaryFactory
{
    /// <summary>The summary of a relocation that moved nothing.</summary>
    public static RuleRelocationSummary None { get; } = new(0, 0);

    /// <summary>Creates the summary; a negative count becomes zero.</summary>
    public static RuleRelocationSummary Create(int relocated, int dropped) => new(Math.Max(relocated, 0), Math.Max(dropped, 0));
}
