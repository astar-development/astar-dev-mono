using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class FakeAutostartRegistration : IAutostartRegistration
{
    public AutostartState State { get; set; } = AutostartStateFactory.CreateDisabled();

    public Task<Result<AutostartState, ErrorResponse>> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<AutostartState, ErrorResponse>(State));

    public Task<Result<AutostartState, ErrorResponse>> EnableAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<AutostartState, ErrorResponse>(State = AutostartStateFactory.CreateEnabled()));

    public Task<Result<AutostartState, ErrorResponse>> DisableAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<AutostartState, ErrorResponse>(State = AutostartStateFactory.CreateDisabled()));
}
