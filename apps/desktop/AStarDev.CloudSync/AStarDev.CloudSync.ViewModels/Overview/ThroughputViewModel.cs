using AStarDev.CloudSync.Core.Overview;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Overview;

/// <summary>Recent transfer rates as shown in the throughput sparkline.</summary>
/// <param name="samples">The samples, oldest first.</param>
/// <param name="localization">Supplies the title and current-rate text.</param>
public sealed class ThroughputViewModel(IReadOnlyList<ThroughputSample> samples, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>Each sample scaled so the peak is 1; all zeros when nothing was transferred.</summary>
    public IReadOnlyList<double> Values { get; } = Scale(samples);

    /// <summary>The sparkline title.</summary>
    public string Title => Localization.GetLocal("Throughput.Title");

    /// <summary>The latest rate, e.g. "14.2 MB/s now", or a no-data message.</summary>
    public string CurrentText => samples.Count == 0
        ? Localization.GetLocal("Throughput.NoData")
        : Localization.GetLocal("Throughput.Current", ByteSizeFormatter.Format(samples[^1].BytesPerSecond, Localization));

    private static double[] Scale(IReadOnlyList<ThroughputSample> samples)
    {
        long peak = samples.Count == 0 ? 0 : samples.Max(sample => sample.BytesPerSecond);

        return peak == 0 ? [.. samples.Select(_ => 0d)] : [.. samples.Select(sample => (double)sample.BytesPerSecond / peak)];
    }
}
