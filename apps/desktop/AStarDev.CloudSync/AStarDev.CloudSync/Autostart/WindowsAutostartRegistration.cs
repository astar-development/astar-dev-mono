using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Autostart;

/// <summary>Registers the app to start at login with a value in the current user's <c>Run</c> key (#1410). The value is the setting: it exists or it does not.</summary>
/// <param name="runKey">Reads and writes the value.</param>
/// <param name="launcher">Supplies the program to start, or nothing for a build that is not installed.</param>
/// <param name="logger">Reports a failure by exception type only.</param>
public sealed class WindowsAutostartRegistration(IRunKeyStore runKey, IAutostartLauncher launcher, ILogger<WindowsAutostartRegistration> logger) : IAutostartRegistration
{
    private const string Location = nameof(WindowsAutostartRegistration);
    private const string ValueName = "AStarDevCloudSync";

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult(launcher.LauncherPath.Match(
            _ => Try.Run(() => runKey.Read(ValueName).Match(_ => AutostartStateFactory.CreateEnabled(), AutostartStateFactory.CreateDisabled)).Match(Succeeded, exception => Failed(exception, "The start at login value could not be read.")),
            () => Succeeded(NotInstalled())));

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> EnableAsync(CancellationToken cancellationToken) =>
        Task.FromResult(launcher.LauncherPath.Match(
            path => Try.Run(() =>
            {
                runKey.Write(ValueName, RunCommandBuilder.Build(path));

                return AutostartStateFactory.CreateEnabled();
            }).Match(Succeeded, exception => Failed(exception, "The start at login value could not be written.")),
            () => Succeeded(NotInstalled())));

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> DisableAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Try.Run(() =>
        {
            runKey.Remove(ValueName);

            return AutostartStateFactory.CreateDisabled();
        }).Match(Succeeded, exception => Failed(exception, "The start at login value could not be removed.")));

    private static Result<AutostartState, ErrorResponse> Succeeded(AutostartState state) => Result.Success<AutostartState, ErrorResponse>(state);

    private static AutostartState NotInstalled() => AutostartStateFactory.CreateNotSupported(AutostartStateFactory.CreateNotInstalledBuild());

    private Result<AutostartState, ErrorResponse> Failed(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<AutostartState, ErrorResponse>(new ErrorResponse(message));
    }
}
