using AStarDev.CloudSync.Classification;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public sealed class FakeClassificationReclassifier : IClassificationReclassifier, IReclassifyRunRecordedSource
{
    public event EventHandler? RunRecorded;

    public int RunRecordedSubscribers => RunRecorded?.GetInvocationList().Length ?? 0;

    public void RaiseRunRecorded() => RunRecorded?.Invoke(this, EventArgs.Empty);

    public ReclassifyOutcome Outcome { get; set; } = ReclassifyOutcomeFactory.CreateCompleted(ReclassifyCountsFactory.Create(0, 0, 0));

    public IReadOnlyList<ReclassifyProgress> Reports { get; set; } = [];

    public TaskCompletionSource? Gate { get; set; }

    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int StartCount { get; private set; }

    public CancellationToken ReceivedToken { get; private set; }

    public bool Throws { get; set; }

    public int AlreadyRunningBeforeSuccess { get; set; }

    public Action? OnWait { get; set; }

    public int WaitCount { get; private set; }

    public List<string> Calls { get; } = [];

    public Task WaitUntilIdleAsync(CancellationToken cancellationToken)
    {
        WaitCount++;
        Calls.Add("wait");
        OnWait?.Invoke();

        return Task.CompletedTask;
    }

    public async Task<ReclassifyOutcome> StartAsync(IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken)
    {
        StartCount++;
        Calls.Add("start");
        if (StartCount <= AlreadyRunningBeforeSuccess)
            return ReclassifyOutcomeFactory.CreateAlreadyRunning();

        ReceivedToken = cancellationToken;
        if (Throws)
            throw new InvalidOperationException("reclassify exploded with C:\\secret\\path");

        foreach (var report in Reports)
            progress.Report(report);

        _ = Started.TrySetResult();
        if (Gate is null)
            return Outcome;

        var wait = Gate.Task.WaitAsync(cancellationToken);
        await ((Task)wait).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return wait.IsCanceled ? ReclassifyOutcomeFactory.CreateCancelled(ReclassifyCountsFactory.Create(0, 0, 0)) : Outcome;
    }
}
