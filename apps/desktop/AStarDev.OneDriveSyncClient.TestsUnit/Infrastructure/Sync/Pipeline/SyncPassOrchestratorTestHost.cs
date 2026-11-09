using AStarDev.Localization;
using AStarDev.LoggingOTel.LogViewer;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

internal static class SyncPassOrchestratorTestHost
{
    private static readonly InMemoryLogProcessor SharedLogProcessor = new();

    public static ISyncPassOrchestrator Create(SyncServiceDependencies dependencies, IAccountRepository accountRepository, IDriveStateRepository driveStateRepository, IFileClassificationRepository classificationRepository, ISettingsService settingsService, ILocalizationService localizationService, ILoggerProvider? loggerProvider = null)
    {
        var repositories = Substitute.For<ISyncPassRepositories>();
        _ = repositories.AccountRepository.Returns(accountRepository);
        _ = repositories.DriveStateRepository.Returns(driveStateRepository);
        _ = repositories.ClassificationRepository.Returns(classificationRepository);

        var services = new ServiceCollection();
        _ = services.AddLogging(builder =>
        {
            _ = builder.SetMinimumLevel(LogLevel.Trace);
            if (loggerProvider is not null)
                _ = builder.AddProvider(loggerProvider);
        });
        _ = services.AddShell(SharedLogProcessor);
        _ = services.AddSingleton(dependencies);
        _ = services.AddSingleton(repositories);
        _ = services.AddSingleton(settingsService);
        _ = services.AddSingleton(localizationService);
        _ = services.AddSingleton(Options.Create(new SyncSettings { ProgressReportInterval = 100 }));

        return services.BuildServiceProvider().GetRequiredService<ISyncPassOrchestrator>();
    }
}
