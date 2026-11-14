using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Tells a pass how many transfers its account may run at once (#1404). A setting that cannot be read never stops the pass: the default is used and a warning is logged.</summary>
/// <param name="reader">Reads the account's setting.</param>
/// <param name="logger">Reports a setting that could not be read, without account details.</param>
internal sealed class TransferLimits(ITransferConcurrencyReader reader, ILogger<TransferLimits> logger)
{
    private const string Location = nameof(TransferLimits);

    internal Task<int> ForAsync(AccountId accountId, CancellationToken cancellationToken) =>
        reader.GetAsync(accountId, cancellationToken).MatchAsync(found => found.Value, _ => Default());

    private int Default()
    {
        LogMessage.Warning(logger, Location, "The account's transfer concurrency could not be read, so the default was used.");

        return TransferConcurrencyFactory.Default;
    }
}
