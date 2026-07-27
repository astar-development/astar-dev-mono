using Azure;
using Azure.Data.Tables;

namespace Fab4Kids.Web.Newsletter;

/// <summary>Azure Table Storage row shape for a <see cref="NewsletterSubscriber"/>.</summary>
public sealed class NewsletterSubscriberEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public DateTimeOffset SubscribedAt { get; set; }
}
