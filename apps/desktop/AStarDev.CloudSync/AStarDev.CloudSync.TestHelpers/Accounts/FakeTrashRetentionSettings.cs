using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeTrashRetentionSettings : ITrashRetentionSettings
{
    public Result<LocalTrashOptions, ErrorResponse> Current { get; set; } = Result.Success<LocalTrashOptions, ErrorResponse>(LocalTrashOptionsFactory.Create());

    public bool SaveFails { get; set; }

    public List<LocalTrashOptions> Saved { get; } = [];

    public Task<Result<LocalTrashOptions, ErrorResponse>> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Current);

    public Task<Result<LocalTrashOptions, ErrorResponse>> SetAsync(LocalTrashOptions options, CancellationToken cancellationToken)
    {
        if (SaveFails)
            return Task.FromResult(Result.Failure<LocalTrashOptions, ErrorResponse>(new ErrorResponse("C:/secret/db could not be written")));

        Saved.Add(options);
        Current = Result.Success<LocalTrashOptions, ErrorResponse>(options);

        return Task.FromResult(Current);
    }
}
