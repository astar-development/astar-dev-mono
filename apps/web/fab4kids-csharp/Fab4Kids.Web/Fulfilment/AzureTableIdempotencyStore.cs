using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Fab4Kids.Web.Fulfilment;

/// <inheritdoc cref="IIdempotencyStore"/>
public sealed class AzureTableIdempotencyStore(ILogger<AzureTableIdempotencyStore> logger, TableClient? tableClient = null) : IIdempotencyStore
{
    private const string PartitionKey = "processed-sessions";

    public Task<Result<bool, string>> TryMarkProcessedAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (tableClient is null)
        {
            LogMessage.Error(logger, "Idempotency storage is not configured (missing connection string or table name).");

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
        }).ToResultAsync(ex =>
        {
            LogMessage.LogException(logger, nameof(AzureTableIdempotencyStore), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return "Something went wrong recording this order.";
        });
    }
}
