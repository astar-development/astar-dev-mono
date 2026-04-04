using System.Diagnostics;

namespace AStar.Dev.Sync.Engine.Features.ProgressTracking;

/// <summary>
///     Calculates estimated time remaining for a sync run (SE-14).
///     ETA is recalculated after each file completes and after each Graph 429 delay.
///     Thread-safe via <see langword="lock"/>.
/// </summary>
public sealed class EtaCalculator
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly object _lock = new();
    private int _filesCompleted;
    private long _additionalDelayMs;

    /// <summary>Records that one more file has completed processing.</summary>
    public void RecordFileCompleted()
    {
        lock (_lock)
        {
            _filesCompleted++;
        }
    }

    /// <summary>Records an additional delay (e.g., a Graph 429 back-off pause) to include in the ETA.</summary>
    public void RecordDelay(TimeSpan delay)
    {
        lock (_lock)
        {
            _additionalDelayMs += (long)delay.TotalMilliseconds;
        }
    }

    /// <summary>
    ///     Calculates the estimated seconds remaining given <paramref name="filesTotal"/>.
    ///     Returns <c>0</c> when no files have been completed yet (avoiding division by zero).
    /// </summary>
    public int CalculateEtaSeconds(int filesTotal)
    {
        lock (_lock)
        {
            if (_filesCompleted == 0 || filesTotal <= 0)

                return 0;

            var elapsedMs = _stopwatch.ElapsedMilliseconds + _additionalDelayMs;
            var msPerFile = elapsedMs / (double)_filesCompleted;
            var remainingFiles = filesTotal - _filesCompleted;

            return (int)Math.Ceiling(msPerFile * remainingFiles / 1_000);
        }
    }
}
