using AStarDev.CloudSync.Classification;

namespace AStarDev.CloudSync.TestsUnit;

internal sealed class StartupFakeClassification : IClassificationStartup
{
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Exception? Throws { get; init; }

    internal bool HoldUntilReleased { get; init; }

    internal bool Finished { get; private set; }

    internal int Runs { get; private set; }

    internal void Release() => release.TrySetResult();

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Runs++;
        if (HoldUntilReleased)
            await release.Task.WaitAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        if (Throws is not null)
            throw Throws;

        Finished = true;
    }
}
