namespace AStar.Dev.FunctionalParadigm;

public static class ResultExtensions
{
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

    public static Task<Result<TResult, TError>> Tap<TResult, TError>(this Task<Result<TResult, TError>> resultTask, Action<TResult> onSuccess, Action<TError>? onFailure = null) => resultTask.ContinueWith(task => task.Result.Tap(onSuccess, onFailure), TaskContinuationOptions.ExecuteSynchronously);

    public static async ValueTask<Result<TResult, TError>> Tap<TResult, TError>(this ValueTask<Result<TResult, TError>> resultTask, Action<TResult> onSuccess, Action<TError>? onFailure = null)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Tap(onSuccess, onFailure);
    }

    public static async Task<Result<TResult, TError>> TapAsync<TResult, TError>(this Task<Result<TResult, TError>> resultTask, Action<TResult> onSuccess, Action<TError>? onFailure = null)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Tap(onSuccess, onFailure);
    }

    public static async ValueTask<Result<TResult, TError>> TapAsync<TResult, TError>(this ValueTask<Result<TResult, TError>> resultTask, Action<TResult> onSuccess, Action<TError>? onFailure = null)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Tap(onSuccess, onFailure);
    }

    public static Result<TMapped, TError> Map<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, TMapped> selector)
        => result switch
            {
                Ok<TResult, TError> ok => new Ok<TMapped, TError>(selector(ok.Value)),
                Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
                _ => throw new InvalidOperationException("Unexpected result type.")
            };

    public static async Task<Result<TMapped, TError>> MapAsync<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, Task<TMapped>> selector) => result switch
    {
        Ok<TResult, TError> ok => new Ok<TMapped, TError>(await selector(ok.Value).ConfigureAwait(false)),
        Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
        _ => throw new InvalidOperationException("Unexpected result type.")
    };

    public static async ValueTask<Result<TMapped, TError>> MapAsync<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, ValueTask<TMapped>> selector) => result switch
    {
        Ok<TResult, TError> ok => new Ok<TMapped, TError>(await selector(ok.Value).ConfigureAwait(false)),
        Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
        _ => throw new InvalidOperationException("Unexpected result type.")
    };

    public static async Task<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, Task<Result<TMapped, TError>>> binder) => result switch
    {
        Ok<TResult, TError> ok => await binder(ok.Value).ConfigureAwait(false),
        Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
        _ => throw new InvalidOperationException("Unexpected result type.")
    };

    public static async Task<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this Task<Result<TResult, TError>> resultTask, Func<TResult, Task<Result<TMapped, TError>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    public static async Task<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this Task<Result<TResult, TError>> resultTask, Func<TResult, ValueTask<Result<TMapped, TError>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    public static async ValueTask<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, ValueTask<Result<TMapped, TError>>> binder) => result switch
    {
        Ok<TResult, TError> ok => await binder(ok.Value).ConfigureAwait(false),
        Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
        _ => throw new InvalidOperationException("Unexpected result type.")
    };

    public static async ValueTask<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this ValueTask<Result<TResult, TError>> resultTask, Func<TResult, Task<Result<TMapped, TError>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    public static async ValueTask<Result<TMapped, TError>> BindAsync<TResult, TMapped, TError>(this ValueTask<Result<TResult, TError>> resultTask, Func<TResult, ValueTask<Result<TMapped, TError>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    public static Result<TMapped, TError> Bind<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, Result<TMapped, TError>> binder)
        => result switch
            {
                Ok<TResult, TError> ok => binder(ok.Value),
                Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
                _ => throw new InvalidOperationException("Unexpected result type.")
            };

    public static Result<TResult, TError> Ensure<TResult, TError>(this Result<TResult, TError> result, Action finallyAction)
    {
        finallyAction();
        return result;
    }

    public static async Task<Result<TResult, TError>> EnsureAsync<TResult, TError>(this Task<Result<TResult, TError>> resultTask, Action finallyAction)
    {
        var result = await resultTask.ConfigureAwait(false);
        finallyAction();
        return result;
    }

    public static async Task<Result<TResult, TError>> EnsureAsync<TResult, TError>(this Task<Result<TResult, TError>> resultTask, Func<ValueTask> finallyAction)
    {
        var result = await resultTask.ConfigureAwait(false);
        await finallyAction().ConfigureAwait(false);
        return result;
    }

    public static async ValueTask<Result<TResult, TError>> EnsureAsync<TResult, TError>(this ValueTask<Result<TResult, TError>> resultTask, Action finallyAction)
    {
        var result = await resultTask.ConfigureAwait(false);
        finallyAction();
        return result;
    }

    public static async ValueTask<Result<TResult, TError>> EnsureAsync<TResult, TError>(this ValueTask<Result<TResult, TError>> resultTask, Func<ValueTask> finallyAction)
    {
        var result = await resultTask.ConfigureAwait(false);
        await finallyAction().ConfigureAwait(false);
        return result;
    }

    public static TOut Match<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, TOut> onSuccess, Func<TError, TOut> onFailure)
        => result switch
            {
                Ok<TResult, TError> ok => onSuccess(ok.Value),
                Fail<TResult, TError> fail => onFailure(fail.Error),
                _ => throw new InvalidOperationException("Unexpected result type.")
            };

    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, Task<TOut>> onSuccess, Func<TError, TOut> onFailure)
        => MatchAsyncCore(result, ok => new ValueTask<TOut>(onSuccess(ok)), fail => new ValueTask<TOut>(onFailure(fail)));

    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, TOut> onSuccess, Func<TError, Task<TOut>> onFailure)
        => MatchAsyncCore(result, ok => new ValueTask<TOut>(onSuccess(ok)), fail => new ValueTask<TOut>(onFailure(fail)));

    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, Task<TOut>> onSuccess, Func<TError, Task<TOut>> onFailure)
        => MatchAsyncCore(result, ok => new ValueTask<TOut>(onSuccess(ok)), fail => new ValueTask<TOut>(onFailure(fail)));

    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, ValueTask<TOut>> onSuccess, Func<TError, TOut> onFailure)
        => MatchAsyncCore(result, onSuccess, fail => new ValueTask<TOut>(onFailure(fail)));

    public static ValueTask<TOut> MatchAsync<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, TOut> onSuccess, Func<TError, ValueTask<TOut>> onFailure)
        => MatchAsyncCore(result, ok => new ValueTask<TOut>(onSuccess(ok)), onFailure);

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
