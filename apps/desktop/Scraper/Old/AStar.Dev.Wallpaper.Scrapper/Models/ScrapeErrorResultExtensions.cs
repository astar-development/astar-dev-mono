using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

/// <summary>
///     Bridges <see cref="Result{TResult,ScrapeError}" /> pipelines into call sites that have not yet migrated off
///     string-typed errors, by mapping the <see cref="ScrapeError" /> down to its <see cref="ScrapeError.Message" />.
/// </summary>
public static class ScrapeErrorResultExtensions
{
    /// <summary>Maps a <see cref="ScrapeError" /> failure down to its message, preserving a success value unchanged.</summary>
    public static Result<TResult, string> ToStringError<TResult>(this Result<TResult, ScrapeError> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Match(value => Result.Success<TResult, string>(value), error => Result.Failure<TResult, string>(error.Message));
    }

    /// <summary>Maps a <see cref="ScrapeError" /> failure down to its message, preserving a success value unchanged.</summary>
    public static async Task<Result<TResult, string>> ToStringError<TResult>(this Task<Result<TResult, ScrapeError>> resultTask)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        return (await resultTask.ConfigureAwait(false)).ToStringError();
    }
}
