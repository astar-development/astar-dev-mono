using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

internal static class StartupServiceExtensions
{
    internal static IServiceCollection AddStartupTasks(this IServiceCollection services)
    {
        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddSingleton<ISpecialFolderResolver, EnvironmentSpecialFolderResolver>();
        _ = services.AddTransient<IStartupTask, DatabaseMigrationStartupTask>();
        _ = services.AddTransient<IStartupTask, TokenValidationStartupTask>();
        _ = services.AddTransient<IStartupTask, SyncStateRecoveryStartupTask>();
        _ = services.AddSingleton<StartupOrchestrator>();

        return services;
    }
}
