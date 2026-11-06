using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Azure;
using Azure.Data.Tables;
using Fab4Kids.Web.Configuration;

namespace Fab4Kids.Web.Newsletter;

/// <inheritdoc cref="INewsletterSubscriberStore"/>
public sealed class AzureTableNewsletterSubscriberStore(ILogger<AzureTableNewsletterSubscriberStore> logger, TableClient? tableClient = null) : INewsletterSubscriberStore
{
    private const string PartitionKey = "subscribers";

    public Task<Result<bool, string>> ExistsAsync(string email, CancellationToken cancellationToken)
    {
        if (tableClient is null)
        {
            ConfigurationLog.FeatureNotConfigured(logger, "Newsletter storage");

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
        }, cancellationToken).ToResultAsync(ex =>
        {
            LogMessage.ExceptionCaught(logger, nameof(AzureTableNewsletterSubscriberStore), ex.ErrorType());

            return "Something went wrong checking your subscription.";
        });
    }

    public Task<Result<Unit, string>> AddAsync(NewsletterSubscriber subscriber, CancellationToken cancellationToken)
    {
        if (tableClient is null)
        {
            ConfigurationLog.FeatureNotConfigured(logger, "Newsletter storage");

            return Task.FromResult<Result<Unit, string>>("Something went wrong. Please try again later.");
        }

        return Try.RunAsync(async () =>
        {
            await tableClient.CreateIfNotExistsAsync(cancellationToken);

            var entity = new NewsletterSubscriberEntity { PartitionKey = PartitionKey, RowKey = RowKeyFor(subscriber.Email), SubscribedAt = subscriber.SubscribedAt };
            await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);

            return Unit.Instance;
        }, cancellationToken).ToResultAsync(ex =>
        {
            LogMessage.ExceptionCaught(logger, nameof(AzureTableNewsletterSubscriberStore), ex.ErrorType());

            return "Something went wrong saving your subscription.";
        });
    }

    private static string RowKeyFor(string email) => email.Trim().ToLowerInvariant();
}
