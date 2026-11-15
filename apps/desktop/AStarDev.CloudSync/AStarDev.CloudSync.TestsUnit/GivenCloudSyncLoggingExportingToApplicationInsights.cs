using AStarDev.CloudSync.Logging;
using AStarDev.CloudSync.Sync;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenCloudSyncLoggingExportingToApplicationInsights : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), $"cloudsync-export-{Guid.NewGuid():N}");
    private readonly AccountLogLevels levels = new();
    private readonly CollectingExporter exporter = new();

    public void Dispose()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    private ServiceProvider Build() => new ServiceCollection()
        .AddLogging(logging =>
        {
            _ = logging.AddOpenTelemetry(options => options.AddProcessor(new SimpleLogRecordExportProcessor(exporter)));
            _ = CloudSyncLogging.Configure(logging, levels, folder);
        })
        .BuildServiceProvider();

    [Fact]
    public void when_entries_of_every_level_are_logged_then_the_exporter_receives_only_warnings_and_above()
    {
        using var provider = Build();
        var logger = provider.GetRequiredService<ILogger<GivenCloudSyncLoggingExportingToApplicationInsights>>();

        LogMessage.Debug(logger, nameof(GivenCloudSyncLoggingExportingToApplicationInsights), "exported debug");
        LogMessage.Information(logger, nameof(GivenCloudSyncLoggingExportingToApplicationInsights), "exported information");
        LogMessage.Warning(logger, nameof(GivenCloudSyncLoggingExportingToApplicationInsights), "exported warning");

        exporter.Exported.ShouldBe([LogLevel.Warning]);
    }

    [Fact]
    public void when_an_account_is_verbose_then_its_debug_and_information_entries_still_never_reach_the_exporter()
    {
        var account = AStarDev.CloudSync.Accounts.AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        levels.Apply(account, AccountLogLevelFactory.CreateVerbose());
        using var provider = Build();
        var logger = provider.GetRequiredService<ILogger<GivenCloudSyncLoggingExportingToApplicationInsights>>();
        using var scope = SyncLogScope.Begin(logger, account);

        LogMessage.Debug(logger, nameof(GivenCloudSyncLoggingExportingToApplicationInsights), "verbose debug");
        LogMessage.Information(logger, nameof(GivenCloudSyncLoggingExportingToApplicationInsights), "verbose information");
        LogMessage.Warning(logger, nameof(GivenCloudSyncLoggingExportingToApplicationInsights), "verbose warning");

        exporter.Exported.ShouldBe([LogLevel.Warning]);
    }

    private sealed class CollectingExporter : BaseExporter<LogRecord>
    {
        private readonly Lock gate = new();
        private readonly List<LogLevel> exported = [];

        public IReadOnlyList<LogLevel> Exported
        {
            get
            {
                lock (gate)
                    return [.. exported];
            }
        }

        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            lock (gate)
                foreach (var record in batch)
                    exported.Add(record.LogLevel);

            return ExportResult.Success;
        }
    }
}
