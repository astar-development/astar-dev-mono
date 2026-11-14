using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>What a single sync run has finished so far, so a run that stops early can still log it (#1578). Created per run and never shared.</summary>
internal sealed class SyncPassTally
{
    /// <summary>The download step's summary once that step has finished, otherwise none.</summary>
    public Option<DownloadPassSummary> Download { get; set; } = Option.None<DownloadPassSummary>();
}
