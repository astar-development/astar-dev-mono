namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Factory methods for creating instances of <see cref="Result{TResult,TError}" />.
/// </summary>
public static class Result
{
    /// <summary>
    ///     Creates a success <see cref="Result{TResult,TError}" /> wrapping the specified value.
    /// </summary>
    public static Result<TResult, TError> Success<TResult, TError>(TResult value) => new Ok<TResult, TError>(value);

    /// <summary>
    ///     Creates a failure <see cref="Result{TResult,TError}" /> wrapping the specified error.
    /// </summary>
    public static Result<TResult, TError> Failure<TResult, TError>(TError error) => new Fail<TResult, TError>(error);
}
