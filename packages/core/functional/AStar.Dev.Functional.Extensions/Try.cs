namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Represents a computation that may succeed or throw an exception.
/// </summary>
public static class Try
{
    /// <summary>
    ///     Runs a function and returns a Result containing the success result or captured exception.
    /// </summary>
    /// <param name="func">The computation to execute.</param>
    /// <returns>A <see cref="Result{TSuccess, Exception}" /> result.</returns>
    public static Result<T, Exception> Run<T>(Func<T> func)
    {
        try
        {
            return new Result<T, Exception>.Ok(func());
        }
        catch(Exception ex)
        {
            return new Result<T, Exception>.Error(ex);
        }
    }

    /// <summary>
    ///     Runs an async function and returns a Result containing the success result or captured exception.
    /// </summary>
    /// <param name="func">The async computation to execute.</param>
    /// <returns>A Task of <see cref="Result{TSuccess, Exception}" /> result.</returns>
    public static async Task<Result<T, Exception>> RunAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return new Result<T, Exception>.Ok(await func());
        }
        catch(Exception ex)
        {
            return new Result<T, Exception>.Error(ex);
        }
    }
}
