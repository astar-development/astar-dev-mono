namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>A single completed transfer, recorded so recent sync throughput can be graphed after a restart.</summary>
public sealed class SyncThroughputSampleEntity
{
    /// <summary>Primary key for the sample.</summary>
    public int Id { get; set; }

    /// <summary>The account the transfer belongs to.</summary>
    public AccountId AccountId { get; set; }

    /// <summary>When the transfer completed.</summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>The number of bytes transferred.</summary>
    public long Bytes { get; set; }

    /// <summary>The owning account.</summary>
    public AccountEntity? Account { get; set; }
}
