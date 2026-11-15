using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Registers the shared update-available dialog service, view-model factory, and notification service.</summary>
public static class UpdateNotificationServiceCollectionExtensions
{
    /// <summary>Registers the shared update-available dialog service, view-model factory, and notification service. Callers must separately register an <see cref="IUpdateDialogTextProvider"/>.</summary>
    public static IServiceCollection AddVelopackUpdateNotifications(this IServiceCollection services) =>
        services
            .AddSingleton<IUpdateApplyGate, ImmediateUpdateApplyGate>()
            .AddSingleton<IUpdateAvailableDialogService, AvaloniaUpdateAvailableDialogService>()
            .AddSingleton<IUpdateAvailableViewModelFactory, UpdateAvailableViewModelFactory>()
            .AddSingleton<IUpdateNotificationService, UpdateNotificationService>();

    /// <summary>Registers the <see cref="IUpdateScheduler"/>, which checks at startup and then hourly. The app must also register an <see cref="IUpdateDeferralStore"/> (see <c>AddVelopackUpdateDeferral</c>) and then start the scheduler. Opt-in, so apps that only check at startup are unchanged.</summary>
    public static IServiceCollection AddVelopackUpdateScheduling(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        return services.AddSingleton<IUpdateScheduler, UpdateScheduler>();
    }
}
