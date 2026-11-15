namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Checks for updates at startup and then every hour, asking the user, reminding them and finally forcing the update, as the update policy decides (#1412).</summary>
public interface IUpdateScheduler
{
    /// <summary>Checks now and then on a timer until <paramref name="cancellationToken"/> is cancelled. A failed check or dialog is logged and the next check still happens. Stopping it never throws.</summary>
    /// <param name="cancellationToken">Stops the schedule.</param>
    Task RunAsync(CancellationToken cancellationToken);
}
