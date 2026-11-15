using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Autostart;

/// <summary>Registers the app to start at login with a value in the current user's <c>Run</c> key (#1410). The value is the setting: it exists or it does not.</summary>
/// <param name="runKey">Reads and writes the value.</param>
/// <param name="launcher">Supplies the program to start, or nothing for a build that is not installed.</param>
/// <param name="logger">Reports a failure by exception type only.</param>
public sealed class WindowsAutostartRegistration(IRunKeyStore runKey, IAutostartLauncher launcher, ILogger<WindowsAutostartRegistration> logger) : IAutostartRegistration
{
    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> GetAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{runKey}{launcher}{logger}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> EnableAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> DisableAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");
}
