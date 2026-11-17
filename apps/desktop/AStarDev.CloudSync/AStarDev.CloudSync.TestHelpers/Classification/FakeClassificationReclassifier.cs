using AStarDev.CloudSync.Classification;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public sealed class FakeClassificationReclassifier : IClassificationReclassifier
{
    public ReclassifyOutcome Outcome { get; set; } = ReclassifyOutcomeFactory.CreateCompleted(ReclassifyCountsFactory.Create(0, 0, 0));

    public IReadOnlyList<ReclassifyProgress> Reports { get; set; } = [];

    public TaskCompletionSource? Gate { get; set; }

    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int StartCount { get; private set; }

    public CancellationToken ReceivedToken { get; private set; }

    public bool Throws { get; set; }

    public async Task<ReclassifyOutcome> StartAsync(IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken)
    {
        StartCount++;
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
