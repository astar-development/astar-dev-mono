using System.IO.Abstractions;
using AStar.Dev.Sync.Engine.Features.Activity;
using AStar.Dev.Sync.Engine.Features.Concurrency;
using AStar.Dev.Sync.Engine.Features.DiskSpace;
using AStar.Dev.Sync.Engine.Features.FileTransfer;
using AStar.Dev.Sync.Engine.Features.LocalScanning;
using AStar.Dev.Sync.Engine.Features.ProgressTracking;
using AStar.Dev.Sync.Engine.Features.Resilience;
using AStar.Dev.Sync.Engine.Features.Scheduling;
using AStar.Dev.Sync.Engine.Features.SyncOrchestration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AStar.Dev.Sync.Engine;

/// <summary>Registers all sync engine services into the DI container.</summary>
public static class SyncEngineServiceExtensions
{
    /// <summary>
    ///     Adds the sync engine, scheduler, progress reporter, and supporting services as singletons,
    ///     with <see cref="IFileTransferService"/> registered as transient (one instance per transfer slot).
    /// </summary>
    public static IServiceCollection AddSyncEngine(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<SyncGate>();
        services.TryAddSingleton<IActivityReporter, NullActivityReporter>();
        services.AddSingleton<ISyncEngine, SyncEngine>();
        services.AddSingleton<ISyncScheduler, SyncScheduler>();
        services.AddSingleton<ISyncProgressReporter, SyncProgressReporter>();
        services.AddSingleton<ExponentialBackoffPolicy>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IDiskSpaceChecker, DiskSpaceChecker>();
        services.AddSingleton<ILocalFileScanner, LocalFileScanner>();
        services.AddTransient<IFileTransferService, FileTransferService>();

        return services;
    }
}
