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

    public Func<string, Result<UploadedFile, ProviderError>> Respond { get; set; } = name => Result.Success<UploadedFile, ProviderError>(UploadedFileFactory.Create(UploadPlanFixture.Id($"id-{name}"), new ContentTag($"c-{name}"), new EntityTag($"e-{name}"), 10, UploadPlanFixture.Hash($"h-{name}")));

    public WriteGate? Gate { get; set; }

    public int Peak => peak;

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
        }

        if (Gate is { } waiting)
            await waiting.PassAsync().WaitAsync(cancellationToken);

        lock (gate)
            inFlight--;

        return Respond(source.Name);
    }
}
