using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

internal static class StartupServiceExtensions
{
    internal static IServiceCollection AddStartupTasks(this IServiceCollection services)
    {
        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddSingleton<IApplicationPathsProvider,  LocalApplicationPathsProvider>();

        return services;
    }
}

