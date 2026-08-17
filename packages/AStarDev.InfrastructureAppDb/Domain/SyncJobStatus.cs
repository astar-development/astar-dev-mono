using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Identity and execution state of a sync job.</summary>
public sealed record SyncJobStatus
{
    /// <summary>Unique identifier for this status record.</summary>
    public Guid Id { get; init; }

    /// <summary>UTC timestamp when the job was queued.</summary>
    public DateTimeOffset QueuedAt { get; init; }

    /// <summary>Current execution state.</summary>
    public SyncJobState State { get; init; } = SyncJobState.Queued;

    /// <summary>Error message, or None if no error.</summary>
    public Option<string> ErrorMessage { get; init; } = Option.None<string>();

    /// <summary>Completion timestamp, or None if not yet completed.</summary>
    public Option<DateTimeOffset> CompletedAt { get; init; } = Option.None<DateTimeOffset>();

    /// <summary>Positional constructor used by the factory.</summary>
    public SyncJobStatus(Guid id, DateTimeOffset queuedAt)
    {
        Id = id;
        QueuedAt = queuedAt;
    }
}
