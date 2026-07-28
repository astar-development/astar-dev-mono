using Azure;
using Azure.Data.Tables;

namespace Fab4Kids.Web.Fulfilment;

/// <summary>Azure Table Storage row shape for a processed Stripe checkout session ID.</summary>
public sealed class ProcessedSessionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}
