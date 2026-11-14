using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScheduleSettings : ISyncScheduleSettings
{
    public Result<SyncSchedule, ErrorResponse> Current { get; set; } = Result.Success<SyncSchedule, ErrorResponse>(SyncScheduleFactory.CreateDefault());

    public Task<Result<SyncSchedule, ErrorResponse>> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Current);

    public Task<Result<SyncSchedule, ErrorResponse>> SetAsync(SyncSchedule schedule, CancellationToken cancellationToken)
    {
        Current = Result.Success<SyncSchedule, ErrorResponse>(schedule);

        return Task.FromResult(Current);
    }
}
