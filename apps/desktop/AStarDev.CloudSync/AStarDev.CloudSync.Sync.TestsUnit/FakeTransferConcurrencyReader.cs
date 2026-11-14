using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeTransferConcurrencyReader : ITransferConcurrencyReader
{
    private int reads;

    public int Value { get; set; } = TransferConcurrencyFactory.Maximum;

    public bool Fails { get; set; }

    public int Reads => Volatile.Read(ref reads);

    public Task<Result<TransferConcurrency, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref reads);

        return Task.FromResult(Fails
            ? Result.Failure<TransferConcurrency, ErrorResponse>(new ErrorResponse("locked"))
            : Result.Success<TransferConcurrency, ErrorResponse>(TransferConcurrencyFactory.Create(Value)));
    }
}
