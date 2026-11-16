using System.IO.Abstractions;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Autostart;

/// <summary>Registers the app to start at login with one file under the user's home folder (#1410). The file is the setting: it exists or it does not.  Reading the state rewrites an existing entry that holds a different program path (#1787). A platform supplies where the file lives and what it holds.</summary>
/// <param name="fileSystem">Reads and writes the file.</param>
/// <param name="home">Supplies the user's home folder.</param>
/// <param name="launcher">Supplies the program to start, or nothing for a build that is not installed.</param>
/// <param name="logger">Reports a failure by exception type only.</param>
public abstract class FileAutostartRegistration(IFileSystem fileSystem, IHomeFolder home, IAutostartLauncher launcher, ILogger logger) : IAutostartRegistration
{
    private const string Location = nameof(FileAutostartRegistration);

    /// <summary>The folders and file name of the entry below the home folder, for example <c>.config</c>, <c>autostart</c>, <c>app.desktop</c>.</summary>
    protected abstract string[] EntryPath { get; }

    /// <summary>Builds the text of the entry that starts <paramref name="launcherPath"/> hidden in the tray.</summary>
    /// <param name="launcherPath">The full path of the program.</param>
    protected abstract string BuildEntry(string launcherPath);

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult(launcher.LauncherPath.Match(
            path => EntryFile().Match(file => file.Exists ? Refresh(file, path) : Succeeded(AutostartStateFactory.CreateDisabled()), NoHome),
            () => Succeeded(NotInstalled())));

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> EnableAsync(CancellationToken cancellationToken) =>
        Task.FromResult(launcher.LauncherPath.Match(
            path => EntryFile().Match(file => Write(file, path), NoHome),
            () => Succeeded(NotInstalled())));

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> DisableAsync(CancellationToken cancellationToken) =>
        Task.FromResult(EntryFile().Match(Remove, NoHome));

    private static Result<AutostartState, ErrorResponse> Succeeded(AutostartState state) => Result.Success<AutostartState, ErrorResponse>(state);

    private static AutostartState NotInstalled() => AutostartStateFactory.CreateNotSupported(AutostartStateFactory.CreateNotInstalledBuild());

    private static Result<AutostartState, ErrorResponse> NoHome() => Result.Failure<AutostartState, ErrorResponse>(new ErrorResponse("The home folder could not be found."));

    private Option<IFileInfo> EntryFile() => home.Folder.Map(folder => fileSystem.FileInfo.New(fileSystem.Path.Join([folder.FullName, .. EntryPath])));

    private Result<AutostartState, ErrorResponse> Refresh(IFileInfo file, string launcherPath) =>
        Try.Run(() => fileSystem.File.ReadAllText(file.FullName) == BuildEntry(launcherPath))
            .Match(current => current ? Succeeded(AutostartStateFactory.CreateEnabled()) : Write(file, launcherPath), _ => Write(file, launcherPath));

    private Result<AutostartState, ErrorResponse> Write(IFileInfo file, string launcherPath) =>
        Try.Run(() =>
        {
            fileSystem.Directory.CreateDirectory(file.DirectoryName ?? string.Empty);
            fileSystem.File.WriteAllText(file.FullName, BuildEntry(launcherPath));

            return AutostartStateFactory.CreateEnabled();
        }).Match(Succeeded, exception => Failed(exception, "The start at login entry could not be written."));

    private Result<AutostartState, ErrorResponse> Remove(IFileInfo file) =>
        Try.Run(() =>
        {
            if (fileSystem.File.Exists(file.FullName))
                fileSystem.File.Delete(file.FullName);

            return AutostartStateFactory.CreateDisabled();
        }).Match(Succeeded, exception => Failed(exception, "The start at login entry could not be removed."));

    private Result<AutostartState, ErrorResponse> Failed(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<AutostartState, ErrorResponse>(new ErrorResponse(message));
    }
}
