using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Testably.Abstractions.Testing;
using TimeProvider = System.TimeProvider;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class OneDriveTransferFixture : IDisposable
{
    public const string Drive = "8A3F2C1D0E9B7A65";
    public const string Token = "secret-token-123";
    public const string Items = "/v1.0/drives/" + Drive + "/items/";

    private readonly InMemoryAccountStore accountStore = new();
    private ServiceProvider? provider;

    public RecordingGraphHandler Graph { get; } = new();

    public FakeAccessTokenSource Tokens { get; } = new();

    public IAccountThrottleGate Gate { get; } = Substitute.For<IAccountThrottleGate>();

    public RecordingLoggerProvider Logs { get; } = new();

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));

    public MockFileSystem FileSystem { get; } = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    public AccountId Account { get; private set; }

    public async Task<OneDriveTransferFixture> BuildAsync(CancellationToken cancellationToken)
    {
        var key = ProviderAccountKeyFactory.Create("key-1").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId(Drive), StorageUsageFactory.Create(1, 2));
        Account = ((AccountAddOutcome.Added)await accountStore.AddAsync(registration, cancellationToken)).AccountId;
        var services = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<TimeProvider>(Time)
            .AddSingleton<IAccountStore>(accountStore)
            .AddSingleton<IAccessTokenSource>(Tokens)
            .AddSingleton(Gate)
            .AddSingleton(new Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options.RetryHandlerOption { Delay = 0, MaxRetry = 1 })
            .AddOneDriveSync();
        _ = services.AddHttpClient(OneDriveSyncHttp.ClientName).ConfigurePrimaryHttpMessageHandler(() => Graph);
        provider = services.BuildServiceProvider();

        return this;
    }

    public IRemoteFileUploader Uploader => provider!.GetRequiredKeyedService<IRemoteFileUploader>(CloudProviderFactory.CreateOneDrive());

    public IRemoteFolderCreator Folders => provider!.GetRequiredKeyedService<IRemoteFolderCreator>(CloudProviderFactory.CreateOneDrive());

    public IRemoteItemRemover Remover => provider!.GetRequiredKeyedService<IRemoteItemRemover>(CloudProviderFactory.CreateOneDrive());

    public System.IO.Abstractions.IFileInfo File(string name, byte[] content)
    {
        FileSystem.File.WriteAllBytes($"/sync/{name}", content);

        return FileSystem.FileInfo.New($"/sync/{name}");
    }

    public void Dispose() => provider?.Dispose();
}
