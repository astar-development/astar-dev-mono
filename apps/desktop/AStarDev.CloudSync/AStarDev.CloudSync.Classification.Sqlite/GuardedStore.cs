using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

internal static class GuardedStore
{
    public static Task<Result<T, ClassificationFailure>> RunAsync<T>(ILogger logger, string location, Func<Task<Result<T, ClassificationFailure>>> work, CancellationToken cancellationToken) =>
        Try.RunAsync(work, cancellationToken).MatchAsync(result => result, exception => Failed<T>(logger, location, exception));

    private static Result<T, ClassificationFailure> Failed<T>(ILogger logger, string location, Exception exception)
    {
        LogMessage.ExceptionCaught(logger, location, exception.ErrorType());

        return Result.Failure<T, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable());
    }
}
