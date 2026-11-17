using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Keeps the latest report of the active reclassify run. Safe to call from any thread. The run that reported last owns the snapshot, and only that run clears it, so a refused second run cannot wipe the first one's progress.</summary>
public sealed class ReclassifyProgressSource : IReclassifyProgressSource
{
    private readonly Lock gate = new();
    private Option<ReclassifyProgress> current = Option.None<ReclassifyProgress>();
    private RunScope? owner;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public Option<ReclassifyProgress> Current
    {
        get
        {
            lock (gate)
                return current;
        }
    }

    /// <summary>Starts tracking one run.</summary>
    /// <param name="downstream">Receives every report after the source has recorded it.</param>
    /// <returns>The scope to pass as the run's progress and to dispose when the run ends.</returns>
    public IReclassifyRunScope Begin(IProgress<ReclassifyProgress> downstream) => new RunScope(this, downstream);

    private void Publish(RunScope scope, ReclassifyProgress report)
    {
        lock (gate)
        {
            if (scope.Ended)
                return;

            owner = scope;
            current = Option.Some(report);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void End(RunScope scope)
    {
        bool cleared;
        lock (gate)
        {
            scope.Ended = true;
            cleared = ReferenceEquals(owner, scope);
            if (cleared)
            {
                owner = null;
                current = Option.None<ReclassifyProgress>();
            }
        }

        if (cleared)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class RunScope(ReclassifyProgressSource source, IProgress<ReclassifyProgress> downstream) : IReclassifyRunScope
    {
        public bool Ended { get; set; }

        public void Report(ReclassifyProgress value)
        {
            source.Publish(this, value);
            downstream.Report(value);
        }

        public void Dispose() => source.End(this);
    }
}
