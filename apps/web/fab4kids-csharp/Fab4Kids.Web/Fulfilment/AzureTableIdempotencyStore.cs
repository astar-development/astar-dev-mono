using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Azure;
using Azure.Data.Tables;
using Fab4Kids.Web.Configuration;

namespace Fab4Kids.Web.Fulfilment;

/// <inheritdoc cref="IIdempotencyStore"/>
public sealed class AzureTableIdempotencyStore(ILogger<AzureTableIdempotencyStore> logger, TableClient? tableClient = null) : IIdempotencyStore
{
    private const string PartitionKey = "processed-sessions";

    public Task<Result<bool, string>> TryMarkProcessedAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (tableClient is null)
        {
            ConfigurationLog.FeatureNotConfigured(logger, "Fulfilment storage");

            return Task.FromResult<Result<bool, string>>("Something went wrong recording this order.");
        }

        return Try.RunAsync(async () =>
        {
            await tableClient.CreateIfNotExistsAsync(cancellationToken);

            var entity = new ProcessedSessionEntity { PartitionKey = PartitionKey, RowKey = sessionId, ProcessedAt = DateTimeOffset.UtcNow };
            try
            {
                await tableClient.AddEntityAsync(entity, cancellationToken);

                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return false;
            }
        }, cancellationToken).ToResultAsync(ex =>
        {
            LogMessage.ExceptionCaught(logger, nameof(AzureTableIdempotencyStore), ex.ErrorType());

            return "Something went wrong recording this order.";
        });
    }
}
