using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeUploader : IRemoteFileUploader
{
    private readonly Lock gate = new();
    private readonly List<(UploadTarget Target, string Name, UploadPrecondition Precondition)> calls = [];
    private int inFlight;
    private int peak;
    private int awaitedInFlight = int.MaxValue;
    private readonly TaskCompletionSource awaitedReached = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Func<string, Result<UploadedFile, ProviderError>> Respond { get; set; } = name => Result.Success<UploadedFile, ProviderError>(UploadedFileFactory.Create(UploadPlanFixture.Id($"id-{name}"), new ContentTag($"c-{name}"), new EntityTag($"e-{name}"), 10, UploadPlanFixture.Hash($"h-{name}")));

    public WriteGate? Gate { get; set; }

    public IReadOnlyList<long> Reports { get; set; } = [];

    public Action BeforeEachReport { get; set; } = () => { };

    public int Peak => peak;

    public Task WhenInFlightAsync(int count)
    {
        lock (gate)
        {
            awaitedInFlight = count;
            if (inFlight >= count)
                awaitedReached.TrySetResult();
        }

        return awaitedReached.Task;
    }

    public IReadOnlyList<(UploadTarget Target, string Name, UploadPrecondition Precondition)> Calls
    {
        get
        {
            lock (gate)
                return [.. calls];
        }
    }

    public async Task<Result<UploadedFile, ProviderError>> UploadAsync(AccountId accountId, UploadTarget target, IFileInfo source, UploadPrecondition precondition, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            calls.Add((target, source.Name, precondition));
            peak = Math.Max(peak, ++inFlight);
            if (inFlight >= awaitedInFlight)
                awaitedReached.TrySetResult();
        }

        if (Gate is { } waiting)
            await waiting.PassAsync().WaitAsync(cancellationToken);

        foreach (var done in Reports)
        {
            BeforeEachReport();
            progress.Report(TransferProgressFactory.Create(done, 10));
        }

        lock (gate)
            inFlight--;

        return Respond(source.Name);
    }
}
