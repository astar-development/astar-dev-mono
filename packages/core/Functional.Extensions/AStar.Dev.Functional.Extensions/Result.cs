namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Represents a discriminated union of success or failure.
/// </summary>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error reason.</typeparam>
public abstract class Result<TSuccess, TError>
{
    private Result()
    {
    }

    /// <summary>
    ///     Matches the result to the appropriate function based on whether it is a success or failure.
    /// </summary>
    /// <typeparam name="TResult">The result type of the match operation.</typeparam>
    /// <param name="onSuccess">Function to apply if the result is successful.</param>
    /// <param name="onFailure">Function to apply if the result is a failure.</param>
    /// <returns>The result of applying the appropriate function.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result is neither a success nor a failure.</exception>
#pragma warning disable S3060 // "is-pattern" should not be used for type-checking
    public TResult Match<TResult>(
        Func<TSuccess, TResult> onSuccess,
        Func<TError, TResult>   onFailure) =>
        this switch
        {
            Ok ok     => onSuccess(ok.Value),
            Error err => onFailure(err.Reason),
            _         => throw new InvalidOperationException($"Unrecognized result type: {GetType().Name}")
        };
#pragma warning restore S3060

    /// <summary>
    ///     Asynchronously matches the result to the appropriate function based on whether it is a success or failure.
    /// </summary>
    /// <typeparam name="TResult">The result type of the match operation.</typeparam>
    /// <param name="onSuccess">Asynchronous function to apply if the result is successful.</param>
    /// <param name="onFailure">Function to apply if the result is a failure.</param>
    /// <returns>A task representing the result of applying the appropriate function.</returns>
#pragma warning disable S3060 // "is-pattern" should not be used for type-checking
    public async Task<TResult> MatchAsync<TResult>(
        Func<TSuccess, Task<TResult>> onSuccess,
        Func<TError, TResult>         onFailure) =>
        this switch
        {
            Ok ok     => await onSuccess(ok.Value),
            Error err => onFailure(err.Reason),
            _         => throw new InvalidOperationException($"Unrecognized result type: {GetType().Name}")
        };
#pragma warning restore S3060

    /// <summary>
    ///     Asynchronously matches the result to the appropriate function based on whether it is a success or failure.
    /// </summary>
    /// <typeparam name="TResult">The result type of the match operation.</typeparam>
    /// <param name="onSuccess">Function to apply if the result is successful.</param>
    /// <param name="onFailure">Asynchronous function to apply if the result is a failure.</param>
    /// <returns>A task representing the result of applying the appropriate function.</returns>
#pragma warning disable S3060 // "is-pattern" should not be used for type-checking
    public async Task<TResult> MatchAsync<TResult>(
        Func<TSuccess, TResult>     onSuccess,
        Func<TError, Task<TResult>> onFailure) =>
        this switch
        {
            Ok ok     => onSuccess(ok.Value),
            Error err => await onFailure(err.Reason),
            _         => throw new InvalidOperationException($"Unrecognized result type: {GetType().Name}")
        };
#pragma warning restore S3060

    /// <summary>
    ///     Asynchronously matches the result to the appropriate function based on whether it is a success or failure.
    /// </summary>
    /// <typeparam name="TResult">The result type of the match operation.</typeparam>
    /// <param name="onSuccess">Asynchronous function to apply if the result is successful.</param>
    /// <param name="onFailure">Asynchronous function to apply if the result is a failure.</param>
    /// <returns>A task representing the result of applying the appropriate function.</returns>
#pragma warning disable S3060 // "is-pattern" should not be used for type-checking
    public async Task<TResult> MatchAsync<TResult>(
        Func<TSuccess, Task<TResult>> onSuccess,
        Func<TError, Task<TResult>>   onFailure) =>
        this switch
        {
            Ok ok     => await onSuccess(ok.Value),
            Error err => await onFailure(err.Reason),
            _         => throw new InvalidOperationException($"Unrecognized result type: {GetType().Name}")
        };

#pragma warning restore S3060

    /// <summary>
    ///     Represents a successful outcome.
    /// </summary>
    public sealed class Ok : Result<TSuccess, TError>
    {
        /// <summary>
        ///     Creates a successful result.
        /// </summary>
        /// <param name="value">The result value.</param>
        public Ok(TSuccess value) => Value = value;

        /// <summary>
        ///     The successful value.
        /// </summary>
        public TSuccess Value { get; }
    }

    /// <summary>
    ///     Represents an error outcome.
    /// </summary>
    public sealed class Error : Result<TSuccess, TError>
    {
        /// <summary>
        ///     Creates an error result.
        /// </summary>
        /// <param name="reason">The failure reason.</param>
        public Error(TError reason) => Reason = reason;

        /// <summary>
        ///     The error reason.
        /// </summary>
        public TError Reason { get; }
    }
}
