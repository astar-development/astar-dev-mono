using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Gives every account the default when no store is registered, so the engine always has a limit (#1404). The persistence registration replaces it.</summary>
internal sealed class DefaultTransferConcurrencyReader : ITransferConcurrencyReader
{
    /// <inheritdoc />
    public Task<Result<TransferConcurrency, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<TransferConcurrency, ErrorResponse>(TransferConcurrencyFactory.CreateDefault()));
}
