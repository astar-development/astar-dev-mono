using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.LogViewer;
using AStar.Dev.OneDrive.Sync.Client.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using WireMock.Server;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public WireMockServer WireMock { get; private set; } = null!;
    public MockFileSystem FileSystem { get; } = new();
    public ServiceProvider Services { get; private set; } = null!;

    private string tempDbPath = string.Empty;

    public async ValueTask InitializeAsync()
    {
        WireMock = WireMockServer.Start(0);
        tempDbPath = Path.GetTempFileName() + ".db";

        var inMemoryLogSink = new InMemoryLogSink();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddPersistence();

        var dbContextDescriptor = services.Single(d => d.ServiceType == typeof(IDbContextFactory<AppDbContext>));
        services.Remove(dbContextDescriptor);
        services.AddSingleton<IDbContextFactory<AppDbContext>>(new TestDbContextFactory(tempDbPath));

        services.AddSingleton(Options.Create(new EntraIdConfiguration
        {
            ClientId = "test-client-id",
            RedirectUri = "http://localhost",
            AuthorityForMicrosoftAccountsOnly = "https://login.microsoftonline.com/consumers",
            Scopes = ["Files.ReadWrite", "offline_access"]
        }));
        services.AddSingleton(Options.Create(new SyncSettings { ProgressReportInterval = 1 }));

        services.AddShell(inMemoryLogSink);

        ReplaceWithStub<IAuthService>(services);
        ReplaceWithStub<IFolderPickerService>(services);
        ReplaceWithStub<IFileManagerService>(services);
        services.AddSingleton(Substitute.For<ILocalizationService>());

        var fileSystemDescriptor = services.Single(d => d.ServiceType == typeof(IFileSystem));
        services.Remove(fileSystemDescriptor);
        services.AddSingleton<IFileSystem>(FileSystem);

        var graphFactoryDescriptor = services.Single(d => d.ServiceType == typeof(IGraphClientFactory));
        services.Remove(graphFactoryDescriptor);
        services.AddSingleton<IGraphClientFactory>(new WireMockGraphClientFactory(WireMock.Url!));

        Services = services.BuildServiceProvider();

        await using var context = Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        WireMock.Stop();
        try { File.Delete(tempDbPath); } catch { }
    }

    private static void ReplaceWithStub<TService>(ServiceCollection services) where TService : class
    {
        var descriptor = services.Single(d => d.ServiceType == typeof(TService));
        services.Remove(descriptor);
        services.AddSingleton(Substitute.For<TService>());
    }

    private sealed class WireMockGraphClientFactory(string baseUrl) : IGraphClientFactory
    {
        public GraphServiceClient CreateClient(Func<CancellationToken, Task<string>> tokenFactory)
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenFactoryProvider(tokenFactory));
            var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
            adapter.BaseUrl = baseUrl;

            return new GraphServiceClient(adapter);
        }

        private sealed class TokenFactoryProvider(Func<CancellationToken, Task<string>> tokenFactory) : IAccessTokenProvider
        {
            public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken ct = default)
                => tokenFactory(ct);

            public AllowedHostsValidator AllowedHostsValidator { get; } = new();
        }
    }
}
