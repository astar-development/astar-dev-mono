using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <inheritdoc />
public sealed class UpdateScheduler(IVelopackUpdateService updates, IUpdatePolicy policy, IUpdateDeferralStore deferralStore, IUpdateAvailableViewModelFactory viewModelFactory, IUpdateAvailableDialogService dialogService, TimeProvider time, ILogger<UpdateScheduler> logger) : IUpdateScheduler
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var wait = await Try.RunAsync(() => CheckOnceAsync(cancellationToken), cancellationToken).MatchAsync(next => next, CheckFailed).ConfigureAwait(false);
                _ = await Task.WhenAny(Task.Delay(wait, time, cancellationToken)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            UpdateSchedulerLog.Stopped(logger);
        }
    }

    private TimeSpan CheckFailed(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, nameof(UpdateScheduler), exception.ErrorType());

        return CheckInterval;
    }

    private async Task<TimeSpan> CheckOnceAsync(CancellationToken cancellationToken)
    {
        var result = await updates.CheckForUpdatesWithResultAsync(cancellationToken).ConfigureAwait(false);
        if (result is not UpdateFound found)
            return CheckInterval;

        var deferral = deferralStore.Load();
        var now = time.GetUtcNow();
        var action = policy.Decide(result, deferral, now);
        if (action is PromptUser)
            return await PromptAsync(found.Info, deferral, cancellationToken).ConfigureAwait(false);

        if (action is ForceAfterWarning)
            return await ForceAsync(found.Info, cancellationToken).ConfigureAwait(false);

        return action is RemindLater remind ? remind.At - now : CheckInterval;
    }

    private async Task<TimeSpan> PromptAsync(UpdateInfo updateInfo, UpdateDeferral deferral, CancellationToken cancellationToken)
    {
        using var viewModel = viewModelFactory.Create(updateInfo);
        await dialogService.ShowAsync(viewModel, cancellationToken).ConfigureAwait(false);
        if (viewModel.Outcome is not Install)
            deferralStore.Save(UpdateDeferralFactory.CreateAfterDeferring(deferral, viewModel.TargetVersion, time.GetUtcNow()));

        return CheckInterval;
    }

    private async Task<TimeSpan> ForceAsync(UpdateInfo updateInfo, CancellationToken cancellationToken)
    {
        using var viewModel = viewModelFactory.CreateForced(updateInfo);
        await dialogService.ShowAsync(viewModel, cancellationToken).ConfigureAwait(false);
        if (viewModel.Outcome is not Install)
            await viewModel.RestartNowCommand.ExecuteAsync(null).ConfigureAwait(false);

        return CheckInterval;
    }
}
