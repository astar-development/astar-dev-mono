using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>State-transition extensions for <see cref="SyncJob"/>.</summary>
public static class SyncJobExtensions
{
    /// <summary>Returns a copy of <paramref name="job"/> transitioned to <see cref="SyncJobState.Completed"/>.</summary>
    public static SyncJob Complete(this SyncJob job) => job with { Status = job.Status with { State = SyncJobState.Completed, CompletedAt = Option.Some(DateTimeOffset.UtcNow) } };

    /// <summary>Returns a copy of <paramref name="job"/> transitioned to <see cref="SyncJobState.Failed"/> with no error message.</summary>
    public static SyncJob Fail(this SyncJob job) => Fail(job, Option.None<string>());

    /// <summary>Returns a copy of <paramref name="job"/> transitioned to <see cref="SyncJobState.Failed"/> with the supplied nullable error message.</summary>
    public static SyncJob Fail(this SyncJob job, string errorMessage) => Fail(job, (Option<string>)errorMessage);

    /// <summary>Returns a copy of <paramref name="job"/> transitioned to <see cref="SyncJobState.Failed"/> with the given error message.</summary>
    public static SyncJob Fail(this SyncJob job, Option<string> errorMessage) => job with { Status = job.Status with { State = SyncJobState.Failed, ErrorMessage = errorMessage, CompletedAt = Option.Some(DateTimeOffset.UtcNow) } };
}
