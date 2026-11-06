using System.Collections.ObjectModel;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.Localization;
using AStarDev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Dashboard.Throughput;

/// <summary>Exposes a bar graph of bytes synced per 10-minute bucket over the past two hours, refreshed every two minutes.</summary>
public sealed partial class ThroughputGraphViewModel : LocalizedObservableObject
{
    private const int BucketCount = 12;
    private static readonly TimeSpan BucketSpan = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private readonly ISyncThroughputRepository repository;
    private readonly IUiTimer timer;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ThroughputGraphViewModel> logger;

    /// <summary>The bars, oldest first.</summary>
    public ObservableCollection<ThroughputBar> Bars { get; } = [];

    /// <summary>Gets a value indicating whether any bytes were synced in the window.</summary>
    [ObservableProperty]
    public partial bool HasData { get; set; }

    /// <summary>Gets the text describing the busiest bucket.</summary>
    [ObservableProperty]
    public partial string PeakText { get; set; } = string.Empty;

    /// <summary>Localised graph title.</summary>
    public string TitleText => Localization.GetLocal("Dashboard.ThroughputTitle");

    /// <summary>Localised empty-state text.</summary>
    public string NoDataText => Localization.GetLocal("Dashboard.ThroughputNoData");

    /// <summary>Creates the view model with twelve empty bars.</summary>
    public ThroughputGraphViewModel(ISyncThroughputRepository repository, IUiTimer timer, TimeProvider timeProvider, ILocalizationService localizationService, ILogger<ThroughputGraphViewModel> logger) : base(localizationService)
    {
        this.repository = repository;
        this.timer = timer;
        this.timeProvider = timeProvider;
        this.logger = logger;

        for (int index = 0; index < BucketCount; index++)
            Bars.Add(ThroughputBarFactory.Create(0, 0, string.Empty));
    }

    /// <summary>Starts the two-minute refresh timer and performs an initial refresh.</summary>
    public void Start()
    {
        timer.Start(RefreshInterval, () => _ = RefreshAsync(CancellationToken.None));
        _ = RefreshAsync(CancellationToken.None);
    }

    /// <summary>Reloads the samples for the past two hours and rebuilds the bars. Failures are logged and leave the previous bars in place.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = timeProvider.GetUtcNow();
            await repository.DeleteOlderThanAsync(now - Retention, cancellationToken);
            var samples = await repository.GetSinceAsync(now - (BucketSpan * BucketCount), cancellationToken);
            var totals = ThroughputBucketer.Bucket(samples, now, BucketCount, BucketSpan);
            long peak = totals.Max();

            Bars.Clear();
            for (int index = 0; index < totals.Count; index++)
                Bars.Add(ThroughputBarFactory.Create(totals[index], peak, totals[index].FileSizeToText()));

            HasData = peak > 0;
            PeakText = peak.FileSizeToText();
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.ThroughputRefreshFailed(logger, ex.GetType().Name);
        }
    }
}
