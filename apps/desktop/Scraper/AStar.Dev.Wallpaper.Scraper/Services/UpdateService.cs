using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Microsoft.Extensions.Options;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace AStar.Dev.Wallpaper.Scraper.Services;

/// <summary>
///     Checks GitHub Releases for a newer Velopack package in the background,
///     downloads it, then prompts the user to restart and apply it.
/// </summary>
public sealed class UpdateService(IOptions<UpdateConfiguration> updateConfiguration, IFileSystem fileSystem)
{
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "astar-dev-wallpaper-scraper-update.log");

    /// <summary>
    ///    Checks for updates in the background, downloads them if available, and prompts the user to restart.
    /// </summary>
    /// <param name="owner">The owner window for the update prompt dialog.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CheckForUpdatesAsync(Window owner)
    {
        Log($"Update check starting; repository {updateConfiguration.Value.RepositoryUrl}");

        await Try.RunAsync(CreateManagerAsync)
            .MapAsync(CheckForUpdateAsync)
            .MapAsync(DownloadUpdateAsync)
            .MapAsync(context => PromptAndApplyAsync(context, owner))
            .TapAsync(static _ => { }, exception => LogUpdateCheckFailure(exception));
    }

    private Task<UpdateCheckContext> CreateManagerAsync()
    {
        var locator = VelopackLocator.CreateDefaultForPlatform(logger: null);
        var manager = new UpdateManager(new GithubSource(updateConfiguration.Value.RepositoryUrl, accessToken: null, prerelease: false), locator: locator);

        return Task.FromResult(new UpdateCheckContext(manager));
    }

    private async Task<UpdateCheckContext> CheckForUpdateAsync(UpdateCheckContext context)
    {
        if (!context.Manager.IsInstalled) return SkipNotInstalled(context);

        Log($"Installed version {context.Manager.CurrentVersion}; checking for updates");
        context.Update = await context.Manager.CheckForUpdatesAsync().ConfigureAwait(false);

        return context.Update is null ? SkipAlreadyLatest(context) : LogUpdateFound(context);
    }

    private async Task<UpdateCheckContext> DownloadUpdateAsync(UpdateCheckContext context)
    {
        if (!context.ShouldContinue) return context;

        await context.Manager.DownloadUpdatesAsync(context.Update!, new DownloadProgressReporter(Log).Report).ConfigureAwait(false);
        Log("Download complete; prompting to restart");

        return context;
    }

    private async Task<UpdateCheckContext> PromptAndApplyAsync(UpdateCheckContext context, Window owner)
    {
        if (!context.ShouldContinue) return context;

        bool restartNow = await Dispatcher.UIThread.InvokeAsync(() => PromptToRestartAsync(owner, context.Update!.TargetFullRelease.Version.ToString())).ConfigureAwait(false);
        Log($"Prompt answered; restart now: {restartNow}");

        if (restartNow) context.Manager.ApplyUpdatesAndRestart(context.Update!);

        return context;
    }

    private UpdateCheckContext SkipNotInstalled(UpdateCheckContext context)
    {
        Log("Not a Velopack install (IDE / plain publish folder run); skipping");
        context.ShouldContinue = false;

        return context;
    }

    private UpdateCheckContext SkipAlreadyLatest(UpdateCheckContext context)
    {
        Log("Already on the latest version");
        context.ShouldContinue = false;

        return context;
    }

    private UpdateCheckContext LogUpdateFound(UpdateCheckContext context)
    {
        Log($"Update {context.Update!.TargetFullRelease.Version} found; downloading");

        return context;
    }

    private void LogUpdateCheckFailure(Exception exception) =>
        Log($"Update check failed: {exception}");

    private void Log(string message)
    {
        try
        {
            fileSystem.File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Diagnostics only - never let logging break the update flow.
        }
    }

    private static async Task<bool> PromptToRestartAsync(Window owner, string version)
    {
        bool restartNow = false;

        var dialog = new Window
        {
            Title = "Update available",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var restartButton = new Button { Content = "Restart now" };
        var laterButton = new Button { Content = "Later" };

        restartButton.Click += (_, _) =>
        {
            restartNow = true;
            dialog.Close();
        };

        laterButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = $"Version {version} has been downloaded. Restart to apply?" },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { laterButton, restartButton },
                },
            },
        };

        await dialog.ShowDialog(owner);

        return restartNow;
    }

    private sealed class UpdateCheckContext(UpdateManager manager)
    {
        public UpdateManager Manager { get; } = manager;

        public UpdateInfo? Update { get; set; }

        public bool ShouldContinue { get; set; } = true;
    }

    private sealed class DownloadProgressReporter(Action<string> log)
    {
        private int lastLoggedProgress;

        public void Report(int progress)
        {
            if (progress < lastLoggedProgress + 25) return;

            lastLoggedProgress = progress;
            log($"Download progress {progress}%");
        }
    }
}
