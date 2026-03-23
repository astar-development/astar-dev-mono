namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Provides pattern-style checks across Option, Result, and Try.
/// </summary>
public static class Pattern
{
    /// <summary>
    ///     Determines if the specified option represents a Some state.
    /// </summary>
    /// <param name="option">The option to evaluate.</param>
    /// <typeparam name="T">The type of the optional value.</typeparam>
    /// <returns>True if the option represents a Some state, false otherwise.</returns>
    public static bool IsSome<T>(Option<T> option) => option is Option<T>.Some;

    /// <summary>
    ///     Determines if the specified option represents a None state.
    /// </summary>
    /// <param name="option">The option to evaluate.</param>
    /// <typeparam name="T">The type of the optional value.</typeparam>
    /// <returns>True if the option represents a None state, false otherwise.</returns>
    public static bool IsNone<T>(Option<T> option) => option is Option<T>.None;

    /// <summary>
    ///     Determines if a result represents a successful state.
    /// </summary>
    /// <param name="result">The result to evaluate.</param>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <returns>True if the result represents a successful state, false otherwise.</returns>
    public static bool IsOk<T, TError>(Result<T, TError> result) => result is Result<T, TError>.Ok;

    /// <summary>
    ///     Determines if a result represents an error state.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <returns>True if the result represents an error state, false otherwise.</returns>
    public static bool IsError<T, TError>(Result<T, TError> result) => result is Result<T, TError>.Error;

    /// <summary>
    ///     Determines if a result from Try.Run is successful.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <returns>True if the result is successful, false otherwise.</returns>
    public static bool IsSuccess<T>(Result<T, Exception> result) => result is Result<T, Exception>.Ok;

    /// <summary>
    ///     Determines if a result from Try.Run is a failure.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <returns>True if the result is a failure, false otherwise.</returns>
    public static bool IsFailure<T>(Result<T, Exception> result) => result is Result<T, Exception>.Error;
}
