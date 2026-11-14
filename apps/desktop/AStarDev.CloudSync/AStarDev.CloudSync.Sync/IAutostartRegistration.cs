using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Registers the app to start when the user logs in, using the operating system's own mechanism (#1410). There is one per platform. The registration is the stored setting: nothing else records it.</summary>
public interface IAutostartRegistration
{
    /// <summary>Reads whether the app is registered.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<Result<AutostartState, ErrorResponse>> GetAsync(CancellationToken cancellationToken);

    /// <summary>Registers the app. It starts hidden in the tray with <c>--minimized</c>. A build that cannot register stays <see cref="AutostartState.NotSupported"/>.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<Result<AutostartState, ErrorResponse>> EnableAsync(CancellationToken cancellationToken);

    /// <summary>Removes the registration. Doing it twice is not an error.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<Result<AutostartState, ErrorResponse>> DisableAsync(CancellationToken cancellationToken);
}
