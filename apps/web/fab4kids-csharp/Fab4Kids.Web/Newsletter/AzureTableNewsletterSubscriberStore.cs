using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Fab4Kids.Web.Newsletter;

/// <inheritdoc cref="INewsletterSubscriberStore"/>
public sealed class AzureTableNewsletterSubscriberStore(ILogger<AzureTableNewsletterSubscriberStore> logger, TableClient? tableClient = null) : INewsletterSubscriberStore
{
    private const string PartitionKey = "subscribers";

    public Task<Result<bool, string>> ExistsAsync(string email, CancellationToken cancellationToken)
    {
        if (tableClient is null)
        {
            LogMessage.Error(logger, "Newsletter subscriber storage is not configured (missing connection string or table name).");

            return Task.FromResult<Result<bool, string>>("Something went wrong. Please try again later.");
        }

        return Try.RunAsync(async () =>
        {
            try
            {
                await tableClient.GetEntityAsync<NewsletterSubscriberEntity>(PartitionKey, RowKeyFor(email), cancellationToken: cancellationToken);

                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }).ToResultAsync(ex =>
        {
            LogMessage.LogException(logger, nameof(AzureTableNewsletterSubscriberStore), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return "Something went wrong checking your subscription.";
        });
    }

    public Task<Result<UnitFp, string>> AddAsync(NewsletterSubscriber subscriber, CancellationToken cancellationToken)
    {
        if (tableClient is null)
        {
            LogMessage.Error(logger, "Newsletter subscriber storage is not configured (missing connection string or table name).");

            return Task.FromResult<Result<UnitFp, string>>("Something went wrong. Please try again later.");
        }

        return Try.RunAsync(async () =>
        {
            await tableClient.CreateIfNotExistsAsync(cancellationToken);

            var entity = new NewsletterSubscriberEntity { PartitionKey = PartitionKey, RowKey = RowKeyFor(subscriber.Email), SubscribedAt = subscriber.SubscribedAt };
            await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);

            return UnitFp.Instance;
        }).ToResultAsync(ex =>
        {
            LogMessage.LogException(logger, nameof(AzureTableNewsletterSubscriberStore), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return "Something went wrong saving your subscription.";
        });
    }

    private static string RowKeyFor(string email) => email.Trim().ToLowerInvariant();
}
