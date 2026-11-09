using System.Runtime.ExceptionServices;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using Microsoft.Kiota.Abstractions;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Graph;

/// <summary>Runs Graph calls through <see cref="Try"/> and maps their failures to fixed text that carries the exception type name and HTTP status only, never a server or exception message (they can hold emails and paths).</summary>
internal static class GraphFailure
{
    /// <summary>Runs <paramref name="graphCall"/>, turning any exception except <see cref="OperationCanceledException"/> and <see cref="SyncReAuthRequiredException"/> into a safe <see cref="Fail{TResult, TError}"/>.</summary>
    internal static Task<Result<T, string>> CaptureAsync<T>(Func<Task<Result<T, string>>> graphCall, CancellationToken cancellationToken)
        => Try.RunAsync(graphCall, cancellationToken).MatchAsync(result => result, ToFail<T>);

    /// <summary>Returns a safe <see cref="Fail{TResult, TError}"/> for <paramref name="exception"/>, rethrowing a <see cref="SyncReAuthRequiredException"/> so re-authentication still reaches the sync pipeline.</summary>
    internal static Result<T, string> ToFail<T>(Exception exception)
    {
        if (exception is SyncReAuthRequiredException)
            ExceptionDispatchInfo.Throw(exception);

        return new Fail<T, string>(Describe(exception));
    }

    private static string Describe(Exception exception) => exception switch
    {
        ApiException apiException => $"{apiException.GetType().Name} ({apiException.ResponseStatusCode})",
        _ => exception.GetType().Name
    };
}
