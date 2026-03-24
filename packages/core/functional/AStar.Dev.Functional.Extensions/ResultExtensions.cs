using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Provides functional operations for transforming and composing <see cref="Result{T, TError}" />.
/// </summary>
[SuppressMessage("ReSharper", "GrammarMistakeInComment")]
public static class ResultExtensions
{
    /// <summary>
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    /// <typeparam name="TError"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="resultTask"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onFailure"></param>
    /// <returns></returns>
    public static async Task<TResult> MatchAsync<TSuccess, TError, TResult>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, Task<TResult>>       onSuccess,
        Func<TError, Task<TResult>>         onFailure)
    {
        Result<TSuccess, TError> result = await resultTask;

        return await result.Match(onSuccess, onFailure);
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    /// <typeparam name="TError"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="resultTask"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onFailure"></param>
    /// <returns></returns>
    public static async Task<TResult> MatchAsync<TSuccess, TError, TResult>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, TResult>             onSuccess,
        Func<TError, TResult>               onFailure)
    {
        Result<TSuccess, TError> result = await resultTask;

        return result.Match(onSuccess, onFailure);
    }

    /// <summary>
    ///     Transforms the success value of a <see cref="Result{TSuccess, TError}" /> using the specified mapping function.
    /// </summary>
    /// <typeparam name="TSuccess">The original type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TNew">The type of the transformed success value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="map">A function that maps the original value to a new value.</param>
    /// <returns>
    ///     A new <see cref="Result{TNew, TError}" /> containing the mapped success value if present,
    ///     or the original error if unsuccessful.
    /// </returns>
    public static Result<TNew, TError> Map<TSuccess, TError, TNew>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, TNew>          map) =>
        result.Match<Result<TNew, TError>>(
                                           ok => new Result<TNew, TError>.Ok(map(ok)),
                                           err => new Result<TNew, TError>.Error(err)
                                          );

    /// <summary>
    ///     Asynchronously transforms the success value of a <see cref="Result{TSuccess, TError}" /> using the specified mapping function.
    /// </summary>
    /// <typeparam name="TSuccess">The original type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TNew">The type of the transformed success value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapAsync">An asynchronous function that maps the original value to a new value.</param>
    /// <returns>
    ///     A task representing a new <see cref="Result{TNew, TError}" /> containing the mapped success value if present,
    ///     or the original error if unsuccessful.
    /// </returns>
    public static async Task<Result<TNew, TError>> MapAsync<TSuccess, TError, TNew>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, Task<TNew>>    mapAsync) =>
        await result.MatchAsync<Result<TNew, TError>>(
                                                      async ok => new Result<TNew, TError>.Ok(await mapAsync(ok)),
                                                      err => new Result<TNew, TError>.Error(err)
                                                     );

    /// <summary>
    ///     Asynchronously transforms the success value of a <see cref="Result{TSuccess, TError}" /> using the specified mapping function.
    /// </summary>
    /// <typeparam name="TSuccess">The original type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TNew">The type of the transformed success value.</typeparam>
    /// <param name="resultTask">A task representing the result to transform.</param>
    /// <param name="map">A function that maps the original value to a new value.</param>
    /// <returns>
    ///     A task representing a new <see cref="Result{TNew, TError}" /> containing the mapped success value if present,
    ///     or the original error if unsuccessful.
    /// </returns>
    public static async Task<Result<TNew, TError>> MapAsync<TSuccess, TError, TNew>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, TNew>                map) =>
        (await resultTask).Map(map);

    /// <summary>
    ///     Asynchronously transforms the success value of a <see cref="Result{TSuccess, TError}" /> using the specified asynchronous mapping function.
    /// </summary>
    /// <typeparam name="TSuccess">The original type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TNew">The type of the transformed success value.</typeparam>
    /// <param name="resultTask">A task representing the result to transform.</param>
    /// <param name="mapAsync">An asynchronous function that maps the original value to a new value.</param>
    /// <returns>
    ///     A task representing a new <see cref="Result{TNew, TError}" /> containing the mapped success value if present,
    ///     or the original error if unsuccessful.
    /// </returns>
    public static async Task<Result<TNew, TError>> MapAsync<TSuccess, TError, TNew>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, Task<TNew>>          mapAsync)
    {
        Result<TSuccess, TError> result = await resultTask;

        return await result.MapAsync(mapAsync);
    }

    /// <summary>
    ///     Transforms the error value of a <see cref="Result{TSuccess, TError}" /> using the specified mapping function.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The original type of the error value.</typeparam>
    /// <typeparam name="TNewError">The type of the transformed error value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapError">A function that maps the original error to a new error.</param>
    /// <returns>
    ///     A new <see cref="Result{TSuccess, TNewError}" /> containing the original success value if present,
    ///     or the mapped error if unsuccessful.
    /// </returns>
    public static Result<TSuccess, TNewError> MapFailure<TSuccess, TError, TNewError>(
        this Result<TSuccess, TError> result,
        Func<TError, TNewError>       mapError) =>
        result.Match<Result<TSuccess, TNewError>>(
                                                  ok => new Result<TSuccess, TNewError>.Ok(ok),
                                                  err => new Result<TSuccess, TNewError>.Error(mapError(err))
                                                 );

    /// <summary>
    ///     Asynchronously transforms the error value of a <see cref="Result{TSuccess, TError}" /> using the specified mapping function.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The original type of the error value.</typeparam>
    /// <typeparam name="TNewError">The type of the transformed error value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapErrorAsync">An asynchronous function that maps the original error to a new error.</param>
    /// <returns>
    ///     A task representing a new <see cref="Result{TSuccess, TNewError}" /> containing the original success value if present,
    ///     or the mapped error if unsuccessful.
    /// </returns>
    public static async Task<Result<TSuccess, TNewError>> MapFailureAsync<TSuccess, TError, TNewError>(
        this Result<TSuccess, TError> result,
        Func<TError, Task<TNewError>> mapErrorAsync) =>
        await result.MatchAsync<Result<TSuccess, TNewError>>(
                                                             ok => new Result<TSuccess, TNewError>.Ok(ok),
                                                             async err => new Result<TSuccess, TNewError>.Error(await mapErrorAsync(err))
                                                            );

    /// <summary>
    ///     Asynchronously transforms the error value of a <see cref="Result{TSuccess, TError}" /> using the specified mapping function.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The original type of the error value.</typeparam>
    /// <typeparam name="TNewError">The type of the transformed error value.</typeparam>
    /// <param name="resultTask">A task representing the result to transform.</param>
    /// <param name="mapError">A function that maps the original error to a new error.</param>
    /// <returns>
    ///     A task representing a new <see cref="Result{TSuccess, TNewError}" /> containing the original success value if present,
    ///     or the mapped error if unsuccessful.
    /// </returns>
    public static async Task<Result<TSuccess, TNewError>> MapFailureAsync<TSuccess, TError, TNewError>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TError, TNewError>             mapError) =>
        (await resultTask).MapFailure(mapError);

    /// <summary>
    ///     Asynchronously transforms the error value of a <see cref="Result{TSuccess, TError}" /> using the specified asynchronous mapping function.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The original type of the error value.</typeparam>
    /// <typeparam name="TNewError">The type of the transformed error value.</typeparam>
    /// <param name="resultTask">A task representing the result to transform.</param>
    /// <param name="mapErrorAsync">An asynchronous function that maps the original error to a new error.</param>
    /// <returns>
    ///     A task representing a new <see cref="Result{TSuccess, TNewError}" /> containing the original success value if present,
    ///     or the mapped error if unsuccessful.
    /// </returns>
    public static async Task<Result<TSuccess, TNewError>> MapFailureAsync<TSuccess, TError, TNewError>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TError, Task<TNewError>>       mapErrorAsync)
    {
        Result<TSuccess, TError> result = await resultTask;

        return await result.MapFailureAsync(mapErrorAsync);
    }

    /// <summary>
    ///     Chains the current result to another <see cref="Result{TNew, TError}" />-producing function,
    ///     allowing for functional composition across result types.
    /// </summary>
    /// <typeparam name="TSuccess">The original type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TNew">The type of the new result's success value.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="bind">A function that returns a new <see cref="Result{TNew, TError}" />.</param>
    /// <returns>
    ///     The result of the binding function if the original was successful; otherwise, the original error.
    /// </returns>
    public static Result<TNew, TError> Bind<TSuccess, TError, TNew>(
        this Result<TSuccess, TError>        result,
        Func<TSuccess, Result<TNew, TError>> bind) =>
        result.Match(
                     bind,
                     err => new Result<TNew, TError>.Error(err)
                    );

    /// <summary>
    ///     Asynchronously chains the current result to another <see cref="Result{TNew, TError}" />-producing function,
    ///     allowing for functional composition across result types.
    /// </summary>
    /// <typeparam name="TSuccess">The original type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TNew">The type of the new result's success value.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="bindAsync">An asynchronous function that returns a new <see cref="Result{TNew, TError}" />.</param>
    /// <returns>
    ///     A task representing the result of the binding function if the original was successful; otherwise, the original error.
    /// </returns>
    public static async Task<Result<TNew, TError>> BindAsync<TSuccess, TError, TNew>(
        this Result<TSuccess, TError>              result,
        Func<TSuccess, Task<Result<TNew, TError>>> bindAsync) =>
        await result.MatchAsync<Result<TNew, TError>>(
                                                      bindAsync,
                                                      err => new Result<TNew, TError>.Error(err)
                                                     );

    /// <summary>
    ///     Asynchronously chains the current result to another <see cref="Result{TNew, TError}" />-producing function,
    ///     allowing for functional composition across result types.
    /// </summary>
    /// <typeparam name="TSuccess">The original type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TNew">The type of the new result's success value.</typeparam>
    /// <param name="resultTask">A task representing the result to bind.</param>
    /// <param name="bind">A function that returns a new <see cref="Result{TNew, TError}" />.</param>
    /// <returns>
    ///     A task representing the result of the binding function if the original was successful; otherwise, the original error.
    /// </returns>
    public static async Task<Result<TNew, TError>> BindAsync<TSuccess, TError, TNew>(
        this Task<Result<TSuccess, TError>>  resultTask,
        Func<TSuccess, Result<TNew, TError>> bind) =>
        (await resultTask).Bind(bind);

    /// <summary>
    ///     Asynchronously chains the current result to another <see cref="Result{TNew, TError}" />-producing function,
    ///     allowing for functional composition across result types.
    /// </summary>
    /// <typeparam name="TSuccess">The original type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TNew">The type of the new result's success value.</typeparam>
    /// <param name="resultTask">A task representing the result to bind.</param>
    /// <param name="bindAsync">An asynchronous function that returns a new <see cref="Result{TNew, TError}" />.</param>
    /// <returns>
    ///     A task representing the result of the binding function if the original was successful; otherwise, the original error.
    /// </returns>
    public static async Task<Result<TNew, TError>> BindAsync<TSuccess, TError, TNew>(
        this Task<Result<TSuccess, TError>>        resultTask,
        Func<TSuccess, Task<Result<TNew, TError>>> bindAsync)
    {
        Result<TSuccess, TError> result = await resultTask;

        return await result.BindAsync(bindAsync);
    }

    /// <summary>
    ///     Executes a side-effect action on the success value of a <see cref="Result{TSuccess, TError}" />,
    ///     returning the original result unchanged.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">An action to invoke if the result is successful.</param>
    /// <returns>
    ///     The original <see cref="Result{TSuccess, TError}" /> instance, unchanged.
    /// </returns>
    public static Result<TSuccess, TError> Tap<TSuccess, TError>(
        this Result<TSuccess, TError> result,
        Action<TSuccess>              action)
    {
        if(result is Result<TSuccess, TError>.Ok ok)
        {
            action(ok.Value);
        }

        return result;
    }

    /// <summary>
    ///     Executes a side-effect action on the error value of a <see cref="Result{TSuccess, TError}" />,
    ///     returning the original result unchanged.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">An action to invoke if the result is a failure.</param>
    /// <returns>
    ///     The original <see cref="Result{TSuccess, TError}" /> instance, unchanged.
    /// </returns>
    public static Result<TSuccess, TError> TapError<TSuccess, TError>(
        this Result<TSuccess, TError> result,
        Action<TError>                action)
    {
        if(result is Result<TSuccess, TError>.Error err)
        {
            action(err.Reason);
        }

        return result;
    }

    /// <summary>
    ///     Asynchronously executes a side-effect action on the success value of a <see cref="Result{TSuccess, TError}" />,
    ///     returning the original result unchanged.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="actionAsync">An asynchronous action to invoke if the result is successful.</param>
    /// <returns>
    ///     A task representing the original <see cref="Result{TSuccess, TError}" /> instance, unchanged.
    /// </returns>
    public static async Task<Result<TSuccess, TError>> TapAsync<TSuccess, TError>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, Task>          actionAsync)
    {
        if(result is Result<TSuccess, TError>.Ok ok)
        {
            await actionAsync(ok.Value);
        }

        return result;
    }

    /// <summary>
    ///     Asynchronously executes a side-effect action on the success value of a <see cref="Result{TSuccess, TError}" />,
    ///     returning the original result unchanged.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">A task representing the result to inspect.</param>
    /// <param name="actionAsync">An asynchronous action to invoke if the result is successful.</param>
    /// <returns>
    ///     A task representing the original <see cref="Result{TSuccess, TError}" /> instance, unchanged.
    /// </returns>
    public static async Task<Result<TSuccess, TError>> TapAsync<TSuccess, TError>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, Task>                actionAsync)
    {
        Result<TSuccess, TError> result = await resultTask;

        return await result.TapAsync(actionAsync);
    }

    /// <summary>
    ///     Executes a side-effect action on the success value of a <see cref="Result{TSuccess, TError}" />,
    ///     returning the original result unchanged.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">A task representing the result to inspect.</param>
    /// <param name="action">An action to invoke if the result is successful.</param>
    /// <returns>
    ///     A task representing the original <see cref="Result{TSuccess, TError}" /> instance, unchanged.
    /// </returns>
    public static async Task<Result<TSuccess, TError>> TapAsync<TSuccess, TError>(
        this Task<Result<TSuccess, TError>> resultTask,
        Action<TSuccess>                    action)
    {
        Result<TSuccess, TError> result = await resultTask;

        return result.Tap(action);
    }

    /// <summary>
    ///     Asynchronously executes a side-effect action on the error value of a <see cref="Result{TSuccess, TError}" />,
    ///     returning the original result unchanged.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="actionAsync">An asynchronous action to invoke if the result is a failure.</param>
    /// <returns>
    ///     A task representing the original <see cref="Result{TSuccess, TError}" /> instance, unchanged.
    /// </returns>
    public static async Task<Result<TSuccess, TError>> TapErrorAsync<TSuccess, TError>(
        this Result<TSuccess, TError> result,
        Func<TError, Task>            actionAsync)
    {
        if(result is Result<TSuccess, TError>.Error err)
        {
            await actionAsync(err.Reason);
        }

        return result;
    }

    /// <summary>
    ///     Executes a side-effect action on the error value of a <see cref="Result{TSuccess, TError}" />,
    ///     returning the original result unchanged.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">A task representing the result to inspect.</param>
    /// <param name="action">An action to invoke if the result is a failure.</param>
    /// <returns>
    ///     A task representing the original <see cref="Result{TSuccess, TError}" /> instance, unchanged.
    /// </returns>
    public static async Task<Result<TSuccess, TError>> TapErrorAsync<TSuccess, TError>(
        this Task<Result<TSuccess, TError>> resultTask,
        Action<TError>                      action)
    {
        Result<TSuccess, TError> result = await resultTask;

        return result.TapError(action);
    }

    /// <summary>
    ///     Asynchronously executes a side-effect action on the error value of a <see cref="Result{TSuccess, TError}" />,
    ///     returning the original result unchanged.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">A task representing the result to inspect.</param>
    /// <param name="actionAsync">An asynchronous action to invoke if the result is a failure.</param>
    /// <returns>
    ///     A task representing the original <see cref="Result{TSuccess, TError}" /> instance, unchanged.
    /// </returns>
    public static async Task<Result<TSuccess, TError>> TapErrorAsync<TSuccess, TError>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TError, Task>                  actionAsync)
    {
        Result<TSuccess, TError> result = await resultTask;

        return await result.TapErrorAsync(actionAsync);
    }
}
