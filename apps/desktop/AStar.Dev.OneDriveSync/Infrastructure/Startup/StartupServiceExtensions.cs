using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

internal static class StartupServiceExtensions
{
    internal static IServiceCollection AddStartupTasks(this IServiceCollection services)
    {
        services.AddTransient<IStartupTask, DatabaseMigrationStartupTask>();
        services.AddTransient<IStartupTask, TokenValidationStartupTask>();
        services.AddTransient<IStartupTask, SyncStateRecoveryStartupTask>();
        services.AddSingleton<StartupOrchestrator>();

        return services;
    }
}
