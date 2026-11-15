using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Logging;
using AStarDev.CloudSync.Sync;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenCloudSyncLogging : IDisposable
{
    private static readonly AccountId Photos = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId Work = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private readonly string folder = Path.Combine(Path.GetTempPath(), $"cloudsync-logging-{Guid.NewGuid():N}");
    private readonly AccountLogLevels levels = new();

    public void Dispose()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    private ServiceProvider Build() => new ServiceCollection().AddLogging(logging => CloudSyncLogging.Configure(logging, levels, folder)).BuildServiceProvider();

    private string Written() => Directory.Exists(folder) ? string.Join('\n', Directory.GetFiles(folder).Select(File.ReadAllText)) : string.Empty;

    private static void LogEverythingFor(ILogger logger, AccountId account, string tag)
    {
        using var scope = SyncLogScope.Begin(logger, account);
        var debug = $"{tag} debug";
        var information = $"{tag} information";
        var warning = $"{tag} warning";
        LogMessage.Debug(logger, nameof(GivenCloudSyncLogging), debug);
        LogMessage.Information(logger, nameof(GivenCloudSyncLogging), information);
        LogMessage.Warning(logger, nameof(GivenCloudSyncLogging), warning);
    }

    [Fact]
    public void when_the_folder_is_asked_then_it_is_the_resolved_log_folder_of_this_platform()
    {
        CloudSyncLogging.FolderPath().ShouldBe(LogFolderResolver.Resolve(LogEnvironmentFactory.CreateCurrent()));
    }

    [Fact]
    public void when_the_limits_are_asked_then_files_are_kept_seven_days_ten_megabytes_each_and_a_hundred_in_total()
    {
        (CloudSyncLogging.Retention, CloudSyncLogging.MaxFileBytes, CloudSyncLogging.MaxTotalBytes).ShouldBe((TimeSpan.FromDays(7), 10_485_760L, 104_857_600L));
    }

    [Fact]
    public void when_an_account_is_verbose_then_its_debug_entries_reach_the_file_and_a_standard_accounts_do_not()
    {
        levels.Apply(Photos, AccountLogLevelFactory.CreateVerbose());
        using var provider = Build();
        var logger = provider.GetRequiredService<ILogger<GivenCloudSyncLogging>>();

        LogEverythingFor(logger, Photos, "photos");
        LogEverythingFor(logger, Work, "work");

        var written = Written();
        (written.Contains("photos debug", StringComparison.Ordinal), written.Contains("photos information", StringComparison.Ordinal), written.Contains("work debug", StringComparison.Ordinal), written.Contains("work information", StringComparison.Ordinal), written.Contains("work warning", StringComparison.Ordinal)).ShouldBe((true, true, false, false, true));
    }

    [Fact]
    public void when_the_level_is_toggled_while_the_app_runs_then_the_next_entries_follow_it()
    {
        using var provider = Build();
        var logger = provider.GetRequiredService<ILogger<GivenCloudSyncLogging>>();

        LogEverythingFor(logger, Photos, "before");
        levels.Apply(Photos, AccountLogLevelFactory.CreateVerbose());
        LogEverythingFor(logger, Photos, "during");

        var written = Written();
        (written.Contains("before debug", StringComparison.Ordinal), written.Contains("during debug", StringComparison.Ordinal)).ShouldBe((false, true));
    }

    [Fact]
    public void when_an_entry_belongs_to_no_account_then_debug_is_dropped_and_information_is_kept()
    {
        using var provider = Build();
        var logger = provider.GetRequiredService<ILogger<GivenCloudSyncLogging>>();

        LogMessage.Debug(logger, nameof(GivenCloudSyncLogging), "app debug");
        LogMessage.Information(logger, nameof(GivenCloudSyncLogging), "app information");

        var written = Written();
        (written.Contains("app debug", StringComparison.Ordinal), written.Contains("app information", StringComparison.Ordinal)).ShouldBe((false, true));
    }

    [Fact]
    public void when_the_logging_is_configured_then_application_insights_only_gets_warnings_and_above()
    {
        using var provider = Build();

        var rules = provider.GetRequiredService<IOptions<LoggerFilterOptions>>().Value.Rules.Where(rule => rule.ProviderName is not null && rule.ProviderName.Contains("OpenTelemetryLoggerProvider", StringComparison.Ordinal)).ToList();

        (rules.Count, rules.Count(rule => rule.LogLevel == LogLevel.Warning || (rule.Filter is not null && rule.Filter(null, null, LogLevel.Information) == false && rule.Filter(null, null, LogLevel.Warning)))).ShouldBe((1, 1));
    }
}
