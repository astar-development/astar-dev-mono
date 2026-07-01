namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///    Provides extension methods for working with <see cref="Result{TResult,TError}" /> instances in a functional programming style.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    ///   Executes the specified <paramref name="onSuccess" /> action if the <paramref name="result" /> is a success, or the <paramref name="onFailure" /> action if it is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to process.</param>
    /// <param name="onSuccess">The action to execute if the result is a success.</param>
    /// <param name="onFailure">The action to execute if the result is a failure.</param>
    /// <returns>The processed result.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Result<TResult, TError> Tap<TResult, TError>(this Result<TResult, TError> result, Action<TResult> onSuccess, Action<TError>? onFailure = null)
    {
        switch (result)
        {
            case Ok<TResult, TError> ok:
                onSuccess(ok.Value);
                return ok;

            case Fail<TResult, TError> fail:
                onFailure?.Invoke(fail.Error);
                return fail;

            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
    }

    /// <summary>
    ///  Executes the specified <paramref name="onSuccess" /> action if the <paramref name="resultTask" /> is a success, or the <paramref name="onFailure" /> action if it is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to process.</param>
    /// <param name="onSuccess">The action to execute if the result is a success.</param>
    /// <param name="onFailure">The action to execute if the result is a failure.</param>
    /// <returns>The processed result.</returns>
    public static Task<Result<TResult, TError>> Tap<TResult, TError>(this Task<Result<TResult, TError>> resultTask, Action<TResult> onSuccess, Action<TError>? onFailure = null) => resultTask.ContinueWith(task => task.Result.Tap(onSuccess, onFailure), TaskContinuationOptions.ExecuteSynchronously);

    /// <summary>
    ///  Executes the specified <paramref name="onSuccess" /> action if the <paramref name="resultTask" /> is a success, or the <paramref name="onFailure" /> action if it is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to process.</param>
    /// <param name="onSuccess">The action to execute if the result is a success.</param>
    /// <param name="onFailure">The action to execute if the result is a failure.</param>
    /// <returns>The processed result.</returns>
    public static async ValueTask<Result<TResult, TError>> TapAsync<TResult, TError>(this ValueTask<Result<TResult, TError>> resultTask, Action<TResult> onSuccess, Action<TError>? onFailure = null)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Tap(onSuccess, onFailure);
    }

    /// <summary>
    /// Executes the specified <paramref name="onSuccess" /> action if the <paramref name="resultTask" /> is a success, or the <paramref name="onFailure" /> action if it is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to process.</param>
    /// <param name="onSuccess">The action to execute if the result is a success.</param>
    /// <param name="onFailure">The action to execute if the result is a failure.</param>
    /// <returns>The processed result.</returns>
    public static async Task<Result<TResult, TError>> TapAsync<TResult, TError>(this Task<Result<TResult, TError>> resultTask, Action<TResult> onSuccess, Action<TError>? onFailure = null)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Tap(onSuccess, onFailure);
    }

    /// <summary>
    ///   Maps the value of a successful <see cref="Result{TResult,TError}" /> to a new value using the specified <paramref name="selector" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="selector">The function to use for mapping.</param>
    /// <returns>The mapped result.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Result<TMapped, TError> Map<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, TMapped> selector)
        => result switch
        {
            Ok<TResult, TError> ok => new Ok<TMapped, TError>(selector(ok.Value)),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException("Unexpected result type.")
        };

    /// <summary>
    ///  Asynchronously maps the value of a successful <see cref="Result{TResult,TError}" /> to a new value using the specified <paramref name="selector" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="selector">The function to use for mapping.</param>
    /// <returns>The mapped result.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<Result<TMapped, TError>> MapAsync<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, Task<TMapped>> selector) => result switch
    {
        Ok<TResult, TError> ok => new Ok<TMapped, TError>(await selector(ok.Value).ConfigureAwait(false)),
        Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
        _ => throw new InvalidOperationException("Unexpected result type.")
    };

    /// <summary>
    ///   Asynchronously maps the value of a successful <see cref="Result{TResult,TError}" /> to a new value using the specified <paramref name="selector" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="selector">The function to use for mapping.</param>
    /// <returns>The mapped result.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async ValueTask<Result<TMapped, TError>> MapAsync<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, ValueTask<TMapped>> selector) => result switch
    {
        Ok<TResult, TError> ok => new Ok<TMapped, TError>(await selector(ok.Value).ConfigureAwait(false)),
        Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
        _ => throw new InvalidOperationException("Unexpected result type.")
    };

    /// <summary>
    ///  Asynchronously binds the value of a successful <see cref="Result{TResult,TError}" /> to a new result using the specified <paramref name="binder" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The function to use for binding.</param>
    /// <returns>The bound result.</returns>
    public static async Task<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, Task<Result<TMapped, TError>>> binder) => result switch
    {
        Ok<TResult, TError> ok => await binder(ok.Value).ConfigureAwait(false),
        Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
        _ => throw new InvalidOperationException("Unexpected result type.")
    };

    /// <summary>
    ///  Asynchronously binds the value of a successful <see cref="Result{TResult,TError}" /> to a new result using the specified <paramref name="binder" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to bind.</param>
    /// <param name="binder">The function to use for binding.</param>
    /// <returns>The bound result.</returns>
    public static async Task<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this Task<Result<TResult, TError>> resultTask, Func<TResult, Task<Result<TMapped, TError>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously binds the value of a successful <see cref="Result{TResult,TError}" /> to a new result using the specified <paramref name="binder" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to bind.</param>
    /// <param name="binder">The function to use for binding.</param>
    /// <returns>The bound result.</returns>
    public static async Task<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this Task<Result<TResult, TError>> resultTask, Func<TResult, ValueTask<Result<TMapped, TError>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    /// <summary>
    ///   Asynchronously binds the value of a successful <see cref="Result{TResult,TError}" /> to a new result using the specified <paramref name="binder" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The function to use for binding.</param>
    /// <returns>The bound result.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async ValueTask<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, ValueTask<Result<TMapped, TError>>> binder) => result switch
    {
        Ok<TResult, TError> ok => await binder(ok.Value).ConfigureAwait(false),
        Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
        _ => throw new InvalidOperationException("Unexpected result type.")
    };

    /// <summary>
    ///  Asynchronously binds the value of a successful <see cref="Result{TResult,TError}" /> to a new result using the specified <paramref name="binder" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to bind.</param>
    /// <param name="binder">The function to use for binding.</param>
    /// <returns>The bound result.</returns>
    public static async ValueTask<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this ValueTask<Result<TResult, TError>> resultTask, Func<TResult, Task<Result<TMapped, TError>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    /// <summary>
    ///  Asynchronously binds the value of a successful <see cref="Result{TResult,TError}" /> to a new result using the specified <paramref name="binder" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to bind.</param>
    /// <param name="binder">The function to use for binding.</param>
    /// <returns>The bound result.</returns>
    public static async ValueTask<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this ValueTask<Result<TResult, TError>> resultTask, Func<TResult, ValueTask<Result<TMapped, TError>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    /// <summary>
    ///   Binds the value of a successful <see cref="Result{TResult,TError}" /> to a new result using the specified <paramref name="binder" /> function, or propagates the failure if the result is a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TMapped">The type of the mapped value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The function to use for binding.</param>
    /// <returns>The bound result.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Result<TMapped, TError> Bind<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, Result<TMapped, TError>> binder)
        => result switch
        {
            Ok<TResult, TError> ok => binder(ok.Value),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException("Unexpected result type.")
        };

    /// <summary>
    ///  Executes the specified <paramref name="finallyAction" /> after the <paramref name="result" /> has been processed, regardless of whether it is a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to process.</param>
    /// <param name="finallyAction">The action to execute after processing the result.</param>
    /// <returns>The processed result.</returns>
    public static Result<TResult, TError> Ensure<TResult, TError>(this Result<TResult, TError> result, Action finallyAction)
    {
        finallyAction();
        return result;
    }

    /// <summary>
    ///     Asynchronously executes the specified <paramref name="finallyAction" /> after the <paramref name="resultTask" /> has been processed, regardless of whether it is a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to process.</param>
    /// <param name="finallyAction">The action to execute after processing the result.</param>
    /// <returns>The processed result.</returns>
    public static async Task<Result<TResult, TError>> EnsureAsync<TResult, TError>(this Task<Result<TResult, TError>> resultTask, Action finallyAction)
    {
        var result = await resultTask.ConfigureAwait(false);
        finallyAction();
        return result;
    }

    /// <summary>
    ///   Asynchronously executes the specified <paramref name="finallyAction" /> after the <paramref name="resultTask" /> has been processed, regardless of whether it is a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to process.</param>
    /// <param name="finallyAction">The action to execute after processing the result.</param>
    /// <returns>The processed result.</returns>
    public static async Task<Result<TResult, TError>> EnsureAsync<TResult, TError>(this Task<Result<TResult, TError>> resultTask, Func<ValueTask> finallyAction)
    {
        var result = await resultTask.ConfigureAwait(false);
        await finallyAction().ConfigureAwait(false);
        return result;
    }

    /// <summary>
    ///  Asynchronously executes the specified <paramref name="finallyAction" /> after the <paramref name="resultTask" /> has been processed, regardless of whether it is a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to process.</param>
    /// <param name="finallyAction">The action to execute after processing the result.</param>
    /// <returns>The processed result.</returns>
    public static async ValueTask<Result<TResult, TError>> EnsureAsync<TResult, TError>(this ValueTask<Result<TResult, TError>> resultTask, Func<ValueTask> finallyAction)
    {
        var result = await resultTask.ConfigureAwait(false);
        await finallyAction().ConfigureAwait(false);
        return result;
    }

    /// <summary>
    ///   Asynchronously executes the specified <paramref name="finallyAction" /> after the <paramref name="resultTask" /> has been processed, regardless of whether it is a success or a failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The task returning the result to process.</param>
    /// <param name="finallyAction">The action to execute after processing the result.</param>
    /// <returns>The processed result.</returns>
    public static async ValueTask<Result<TResult, TError>> EnsureAsync<TResult, TError>(this ValueTask<Result<TResult, TError>> resultTask, Action finallyAction)
    {
        var result = await resultTask.ConfigureAwait(false);
        finallyAction();
        return result;
    }

    /// <summary>
    ///   Matches the <paramref name="result" /> and executes the corresponding function based on whether it is a success or a failure, returning the result of the executed function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute if the result is a success.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The result of the executed function.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static TOut Match<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, TOut> onSuccess, Func<TError, TOut> onFailure)
        => result switch
        {
            Ok<TResult, TError> ok => onSuccess(ok.Value),
            Fail<TResult, TError> fail => onFailure(fail.Error),
            _ => throw new InvalidOperationException("Unexpected result type.")
        };

    /// <summary>
    ///  Asynchronously matches the <paramref name="result" /> and executes the corresponding function based on whether it is a success or a failure, returning the result of the executed function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute if the result is a success.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The result of the executed function.</returns>
    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, Task<TOut>> onSuccess, Func<TError, TOut> onFailure)
        => MatchAsyncCore(result, ok => new ValueTask<TOut>(onSuccess(ok)), fail => new ValueTask<TOut>(onFailure(fail)));

    /// <summary>
    ///  Asynchronously matches the <paramref name="result" /> and executes the corresponding function based on whether it is a success or a failure, returning the result of the executed function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute if the result is a success.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The result of the executed function.</returns>
    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, TOut> onSuccess, Func<TError, Task<TOut>> onFailure)
        => MatchAsyncCore(result, ok => new ValueTask<TOut>(onSuccess(ok)), fail => new ValueTask<TOut>(onFailure(fail)));

    /// <summary>
    ///   Asynchronously matches the <paramref name="result" /> and executes the corresponding function based on whether it is a success or a failure, returning the result of the executed function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute if the result is a success.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The result of the executed function.</returns>
    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, Task<TOut>> onSuccess, Func<TError, Task<TOut>> onFailure)
        => MatchAsyncCore(result, ok => new ValueTask<TOut>(onSuccess(ok)), fail => new ValueTask<TOut>(onFailure(fail)));

    /// <summary>
    ///  Asynchronously matches the <paramref name="result" /> and executes the corresponding function based on whether it is a success or a failure, returning the result of the executed function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute if the result is a success.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The result of the executed function.</returns>
    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, ValueTask<TOut>> onSuccess, Func<TError, TOut> onFailure)
        => MatchAsyncCore(result, onSuccess, fail => new ValueTask<TOut>(onFailure(fail)));

    /// <summary>
    ///  Asynchronously matches the <paramref name="result" /> and executes the corresponding function based on whether it is a success or a failure, returning the result of the executed function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute if the result is a success.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The result of the executed function.</returns>
    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, TOut> onSuccess, Func<TError, ValueTask<TOut>> onFailure)
        => MatchAsyncCore(result, ok => new ValueTask<TOut>(onSuccess(ok)), onFailure);

    /// <summary>
    ///  Asynchronously matches the <paramref name="result" /> and executes the corresponding function based on whether it is a success or a failure, returning the result of the executed function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute if the result is a success.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The result of the executed function.</returns>
    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, ValueTask<TOut>> onSuccess, Func<TError, ValueTask<TOut>> onFailure)
        => MatchAsyncCore(result, onSuccess, onFailure);

    private static ValueTask<TOut> MatchAsyncCore<TResult, TError, TOut>(Result<TResult, TError> result, Func<TResult, ValueTask<TOut>> onSuccess, Func<TError, ValueTask<TOut>> onFailure)
        => result switch
        {
            Ok<TResult, TError> ok => onSuccess(ok.Value),
            Fail<TResult, TError> fail => onFailure(fail.Error),
            _ => throw new InvalidOperationException("Unexpected result type.")
        };
}
