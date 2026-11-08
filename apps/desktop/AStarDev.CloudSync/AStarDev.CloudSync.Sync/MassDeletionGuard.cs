namespace AStarDev.CloudSync.Sync;

/// <summary>The per-pair mass-deletion guard (#1399 decision 6): a pair's deletions are held when more than 25% of its baseline items and at least 50 items are missing at once. Smaller pairs are protected by the path quarantine instead.</summary>
public static class MassDeletionGuard
{
    /// <summary>The smallest number of missing items that can trip the guard.</summary>
    public const int MinimumMissingItems = 50;

    /// <summary>The share of the baseline, in percent, that must be exceeded to trip the guard.</summary>
    public const int ThresholdPercent = 25;

    /// <summary>Decides whether the pair's deletions may go ahead. Negative counts are treated as zero.</summary>
    /// <param name="baselineItems">The number of items in the pair's baseline.</param>
    /// <param name="missingItems">The number of baseline items missing in this pass.</param>
    public static MassDeletionVerdict Evaluate(int baselineItems, int missingItems)
    {
        long baseline = Math.Max(baselineItems, 0);
        long missing = Math.Max(missingItems, 0);

        return missing >= MinimumMissingItems && missing * 100 > baseline * ThresholdPercent
            ? MassDeletionVerdictFactory.CreateHeld()
            : MassDeletionVerdictFactory.CreateAllowed();
    }
}
