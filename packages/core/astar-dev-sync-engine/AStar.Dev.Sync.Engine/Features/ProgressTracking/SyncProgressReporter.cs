using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace AStar.Dev.Sync.Engine.Features.ProgressTracking;

/// <inheritdoc />
public sealed class SyncProgressReporter : ISyncProgressReporter, IDisposable
{
    private readonly ConcurrentDictionary<string, Subject<SyncProgress>> _subjects = new();

    /// <inheritdoc />
    public IObservable<SyncProgress> GetProgressStream(string accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        return _subjects.GetOrAdd(accountId, _ => new Subject<SyncProgress>());
    }

    /// <inheritdoc />
    public void Report(string accountId, SyncProgress progress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(progress);

        if (_subjects.TryGetValue(accountId, out var subject))
            subject.OnNext(progress);
    }

    /// <summary>Completes all subjects and releases resources.</summary>
    public void Dispose()
    {
        foreach (var subject in _subjects.Values)
        {
            subject.OnCompleted();
            subject.Dispose();
        }
    }
}
