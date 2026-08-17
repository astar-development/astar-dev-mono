using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Registers the shared update-available dialog service, view-model factory, and notification service.</summary>
public static class UpdateNotificationServiceCollectionExtensions
{
    /// <summary>Registers the shared update-available dialog service, view-model factory, and notification service. Callers must separately register an <see cref="IUpdateDialogTextProvider"/>.</summary>
    public static IServiceCollection AddVelopackUpdateNotifications(this IServiceCollection services) =>
        services
            .AddSingleton<IUpdateAvailableDialogService, AvaloniaUpdateAvailableDialogService>()
            .AddSingleton<IUpdateAvailableViewModelFactory, UpdateAvailableViewModelFactory>()
            .AddSingleton<IUpdateNotificationService, UpdateNotificationService>();
}
