using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FileAuditFixture : IDisposable
{
    public const string Root = "/sync/root";

    public static readonly AccountId Account = SelectionTestRules.Account;

    private readonly ServiceProvider provider;

    public FileAuditFixture()
    {
        Roots.Root = Option.Some(FileSystem.Directory.CreateDirectory(Root));
        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<ISyncRootReader>(Roots)
            .AddSingleton<ISelectionRuleReader>(Rules)
            .AddSingleton<IProviderCapability<IRemoteChangeFeed>>(new SingleProviderCapability<IRemoteChangeFeed>(Feed))
            .AddCloudSync()
            .BuildServiceProvider();
    }

    public MockFileSystem FileSystem { get; } = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    public FixedSyncRootReader Roots { get; } = new();

    public ScriptedRuleReader Rules { get; } = new();

    public ScriptedChangeFeed Feed { get; } = new();

    public RecordingLoggerProvider Logs { get; } = new();

    public IFileAuditor Auditor => provider.GetRequiredService<IFileAuditor>();

    public void Include(params string[] folders) => Rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([.. folders.Select(folder => SelectionTestRules.Include(folder))]);

    public void Write(string relative)
    {
        var full = $"{Root}/{relative}";
        _ = FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(full)!);
        FileSystem.File.WriteAllText(full, "content");
    }

    public async Task<FileAuditCounts> CountsAsync(CancellationToken cancellationToken) =>
        (await Auditor.AuditAsync(Account, cancellationToken)).Match(counts => counts, failure => throw new InvalidOperationException($"The audit failed: {failure.GetType().Name}."));

    public void Dispose() => provider.Dispose();
}
