using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSyncScheduleSettings : ISyncScheduleSettings
{
    public Result<SyncSchedule, ErrorResponse> Current { get; set; } = Result.Success<SyncSchedule, ErrorResponse>(SyncScheduleFactory.CreateDefault());

    public bool SaveFails { get; set; }

    public List<SyncSchedule> Saved { get; } = [];

    public Task<Result<SyncSchedule, ErrorResponse>> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Current);

    public Task<Result<SyncSchedule, ErrorResponse>> SetAsync(SyncSchedule schedule, CancellationToken cancellationToken)
    {
        if (SaveFails)
            return Task.FromResult(Result.Failure<SyncSchedule, ErrorResponse>(new ErrorResponse("C:/secret/db could not be written")));

        Saved.Add(schedule);
        Current = Result.Success<SyncSchedule, ErrorResponse>(schedule);

        return Task.FromResult(Current);
    }
}
