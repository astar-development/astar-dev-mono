namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadPassSummary"/> values.</summary>
public static class UploadPassSummaryFactory
{
    /// <summary>Wraps the outcomes of a pass.</summary>
    public static UploadPassSummary Create(IEnumerable<UploadItemOutcome> outcomes) => new([.. outcomes]);
}
