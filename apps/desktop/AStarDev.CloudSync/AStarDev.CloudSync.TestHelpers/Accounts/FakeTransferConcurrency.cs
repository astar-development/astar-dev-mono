using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeTransferConcurrency : ITransferConcurrencyReader, ITransferConcurrencyWriter
{
    private readonly List<(AccountId AccountId, int Value)> saved = [];

    public int Stored { get; set; } = TransferConcurrencyFactory.Default;

    public bool ReadFails { get; set; }

    public bool WriteFails { get; set; }

    public IReadOnlyList<(AccountId AccountId, int Value)> Saved => saved;

    public Task<Result<TransferConcurrency, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(ReadFails
            ? Result.Failure<TransferConcurrency, ErrorResponse>(new ErrorResponse("locked"))
            : Result.Success<TransferConcurrency, ErrorResponse>(TransferConcurrencyFactory.Create(Stored)));

    public Task<Result<Unit, ErrorResponse>> SetAsync(AccountId accountId, TransferConcurrency concurrency, CancellationToken cancellationToken)
    {
        if (WriteFails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        saved.Add((accountId, concurrency.Value));
        Stored = concurrency.Value;

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
