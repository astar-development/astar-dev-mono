using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

internal static class StartupServiceExtensions
{
    internal static IServiceCollection AddStartupTasks(this IServiceCollection services)
    {
        _ = services.AddSingleton<IFileSystem, FileSystem>();

        return services;
    }
}

