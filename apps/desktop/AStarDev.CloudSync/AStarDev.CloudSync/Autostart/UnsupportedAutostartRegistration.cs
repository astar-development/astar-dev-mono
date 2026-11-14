using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Autostart;

/// <summary>The registration for platforms that have none yet (#1410): the state is always not supported.</summary>
[ExcludeFromCodeCoverage(Justification = "A constant answer.")]
internal sealed class UnsupportedAutostartRegistration : IAutostartRegistration
{
    private static readonly Task<Result<AutostartState, ErrorResponse>> Answer = Task.FromResult(Result.Success<AutostartState, ErrorResponse>(AutostartStateFactory.CreateNotSupported(AutostartStateFactory.CreatePlatformNotSupported())));

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> GetAsync(CancellationToken cancellationToken) => Answer;

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> EnableAsync(CancellationToken cancellationToken) => Answer;

    /// <inheritdoc />
    public Task<Result<AutostartState, ErrorResponse>> DisableAsync(CancellationToken cancellationToken) => Answer;
}
