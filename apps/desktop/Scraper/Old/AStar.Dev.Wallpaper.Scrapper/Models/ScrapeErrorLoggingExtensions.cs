using AStar.Dev.FunctionalParadigm;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

/// <summary>Logging extensions for <see cref="Result{TResult,TError}" /> pipelines that fail with a <see cref="ScrapeError" />.</summary>
public static class ScrapeErrorLoggingExtensions
{
    /// <summary>Logs an error when <paramref name="result" /> is a failure, then returns <paramref name="result" /> unchanged.</summary>
    public static Result<TResult, ScrapeError> LogFailure<TResult>(this Result<TResult, ScrapeError> result, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(logger);

        return result.Tap(_ => { }, error => logger.Error("Scrape pipeline failure: {Message}", error.Message));
    }

    /// <summary>Logs an error when the awaited result is a failure, then returns the result unchanged.</summary>
    public static Task<Result<TResult, ScrapeError>> LogFailure<TResult>(this Task<Result<TResult, ScrapeError>> resultTask, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        return resultTask.TapAsync(_ => { }, error => logger.Error("Scrape pipeline failure: {Message}", error.Message));
    }
}
