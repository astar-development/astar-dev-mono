using AStarDev.LoggingOTel.LogViewer;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Startup;
using AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Graph;

internal sealed class GraphServiceTestHost : IDisposable
{
    private readonly InMemoryLogProcessor inMemoryLogProcessor = new();
    private readonly ServiceProvider serviceProvider;

    public GraphServiceTestHost(WireMockServer server)
    {
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(LoggerProvider));
        _ = services.AddShell(inMemoryLogProcessor);
        _ = services.AddSingleton<IGraphClientFactory>(new WireMockGraphClientFactory(server));
        _ = services.AddSingleton(Substitute.For<IUploadService>());
        serviceProvider = services.BuildServiceProvider();
    }

    public RecordingLoggerProvider LoggerProvider { get; } = new();

    public IGraphService GraphService => serviceProvider.GetRequiredService<IGraphService>();

    public void Dispose()
    {
        serviceProvider.Dispose();
        inMemoryLogProcessor.Dispose();
    }
}
