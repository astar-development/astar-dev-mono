using AStarDev.FunctionalParadigm;

namespace AStarDev.FunctionalParadigm;

/// <summary>Provides extension methods for working with <see cref="Result{TResult, TError}"/> instances.</summary>
public static class ResultExtensions
{
    private const string UnexpectedResultTypeMessage = "Unexpected result type.";

    extension<TResult, TError>(Result<TResult, TError> result)
    {
        /// <summary>Executes the specified actions based on the result's success or failure state.</summary>
        /// <param name="onSuccess">The action to execute if the result is successful.</param>
        /// <param name="onFailure">The action to execute if the result is a failure.</param>
        /// <returns>The original result instance.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Result<TResult, TError> Tap(Action<TResult> onSuccess, Action<TError>? onFailure = null)
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
                    throw new InvalidOperationException(UnexpectedResultTypeMessage);
            }
        }

/// <summary>Executes the specified action if the result is a failure.</summary>
/// <param name="onFailure">The action to execute if the result is a failure.</param>
/// <returns>The original result instance.</returns>
        public Result<TResult, TError> TapError(Action<TError> onFailure)
        {
            if (result is Fail<TResult, TError> fail) onFailure(fail.Error);

            return result;
        }

/// <summary>Executes the specified action asynchronously if the result is successful.</summary>
/// <param name="onSuccessAsync">The asynchronous action to execute if the result is successful.</param>
/// <returns>The original result instance.</returns>
        public async Task<Result<TResult, TError>> TapAsync(Func<TResult, Task> onSuccessAsync)
        {
            if (result is Ok<TResult, TError> ok) await onSuccessAsync(ok.Value).ConfigureAwait(false);

            return result;
        }

/// <summary>Executes the specified function to transform the result's value if it is successful.</summary>
/// <typeparam name="TMapped">The type to which the result's value will be transformed.</typeparam>
/// <param name="selector">The function to transform the result's value.</param>
/// <returns>A new result instance with the transformed value if successful, or the original failure.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public Result<TMapped, TError> Map<TMapped>(Func<TResult, TMapped> selector)
            => result switch
            {
                Ok<TResult, TError> ok => new Ok<TMapped, TError>(selector(ok.Value)),
                Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

/// <summary>Executes the specified function asynchronously to transform the result's value if it is successful.</summary>
/// <typeparam name="TMapped">The type to which the result's value will be transformed.</typeparam>
/// <param name="selector">The asynchronous function to transform the result's value.</param>
/// <returns>A new result instance with the transformed value if successful, or the original failure.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public async Task<Result<TMapped, TError>> MapAsync<TMapped>(Func<TResult, Task<TMapped>> selector) => result switch
        {
            Ok<TResult, TError> ok => new Ok<TMapped, TError>(await selector(ok.Value).ConfigureAwait(false)),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
        };

/// <summary>Executes the specified function asynchronously to transform the result's value if it is successful.</summary>
/// <typeparam name="TMapped">The type to which the result's value will be transformed.</typeparam>
/// <param name="selector">The asynchronous function to transform the result's value.</param>
/// <returns>A new result instance with the transformed value if successful, or the original failure.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public async ValueTask<Result<TMapped, TError>> MapAsync<TMapped>(Func<TResult, ValueTask<TMapped>> selector) => result switch
        {
            Ok<TResult, TError> ok => new Ok<TMapped, TError>(await selector(ok.Value).ConfigureAwait(false)),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
        };

/// <summary>Executes the specified function asynchronously to bind the result's value if it is successful.</summary>
/// <typeparam name="TMapped">The type to which the result's value will be bound.</typeparam>
/// <param name="binder">The asynchronous function to bind the result's value.</param>
/// <returns>A new result instance with the bound value if successful, or the original failure.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public async Task<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, Task<Result<TMapped, TError>>> binder) => result switch
        {
            Ok<TResult, TError> ok => await binder(ok.Value).ConfigureAwait(false),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
        };

/// <summary>Executes the specified function asynchronously to bind the result's value if it is successful, using a ValueTask.</summary>
/// <typeparam name="TMapped">The type to which the result's value will be bound.</typeparam>
/// <param name="binder">The asynchronous function to bind the result's value.</param>
/// <returns>A new result instance with the bound value if successful, or the original failure.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public async ValueTask<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, ValueTask<Result<TMapped, TError>>> binder) => result switch
        {
            Ok<TResult, TError> ok => await binder(ok.Value).ConfigureAwait(false),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
        };

/// <summary>Executes the specified function to bind the result's value if it is successful.</summary>
/// <typeparam name="TMapped">The type to which the result's value will be bound.</typeparam>
/// <param name="binder">The function to bind the result's value.</param>
/// <returns>A new result instance with the bound value if successful, or the original failure.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public Result<TMapped, TError> Bind<TMapped>(Func<TResult, Result<TMapped, TError>> binder)
            => result switch
            {
                Ok<TResult, TError> ok => binder(ok.Value),
                Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

/// <summary>Executes the specified action regardless of the result's success or failure, and then returns the original result.</summary>
/// <param name="finallyAction">The action to execute regardless of the result's success or failure.</param>
/// <returns>The original result instance.</returns>
        public Result<TResult, TError> Ensure(Action finallyAction)
        {
            finallyAction();

            return result;
        }

/// <summary>Executes the specified function based on the result's success or failure, and returns the corresponding value.</summary>
/// <typeparam name="TOut">The type of the value returned by the match functions.</typeparam>
/// <param name="onSuccess">The function to execute if the result is successful.</param>
/// <param name="onFailure">The function to execute if the result is a failure.</param>
/// <returns>The value returned by the corresponding match function.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public TOut Match<TOut>(Func<TResult, TOut> onSuccess, Func<TError, TOut> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => onSuccess(ok.Value),
                Fail<TResult, TError> fail => onFailure(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

/// <summary>Executes the specified asynchronous function based on the result's success or failure, and returns the corresponding value.</summary>
/// <typeparam name="TOut">The type of the value returned by the match functions.</typeparam>
/// <param name="onSuccess">The asynchronous function to execute if the result is successful.</param>
/// <param name="onFailure">The asynchronous function to execute if the result is a failure.</param>
/// <returns>The value returned by the corresponding match function.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public Task<TOut> MatchAsync<TOut>(Func<TResult, Task<TOut>> onSuccess, Func<TError, Task<TOut>> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => onSuccess(ok.Value),
                Fail<TResult, TError> fail => onFailure(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

/// <summary>Executes the specified asynchronous function based on the result's success or failure, and returns the corresponding value.</summary>
/// <typeparam name="TOut">The type of the value returned by the match functions.</typeparam>
/// <param name="onSuccess">The asynchronous function to execute if the result is successful.</param>
/// <param name="onFailure">The asynchronous function to execute if the result is a failure.</param>
/// <returns>The value returned by the corresponding match function.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public ValueTask<TOut> MatchAsync<TOut>(Func<TResult, ValueTask<TOut>> onSuccess, Func<TError, ValueTask<TOut>> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => onSuccess(ok.Value),
                Fail<TResult, TError> fail => onFailure(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

/// <summary>Executes the specified asynchronous function based on the result's success or failure, and returns the corresponding value.</summary>
/// <typeparam name="TOut">The type of the value returned by the match functions.</typeparam>
/// <param name="onSuccess">The asynchronous function to execute if the result is successful.</param>
/// <param name="onFailure">The function to execute if the result is a failure.</param>
/// <returns>The value returned by the corresponding match function.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public ValueTask<TOut> MatchAsync<TOut>(Func<TResult, ValueTask<TOut>> onSuccess, Func<TError, TOut> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => onSuccess(ok.Value),
                Fail<TResult, TError> fail => ValueTask.FromResult(onFailure(fail.Error)),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

/// <summary>Executes the specified asynchronous function based on the result's success or failure, and returns the corresponding value.</summary>
/// <typeparam name="TOut">The type of the value returned by the match functions.</typeparam>
/// <param name="onSuccess">The function to execute if the result is successful.</param>
/// <param name="onFailure">The asynchronous function to execute if the result is a failure.</param>
/// <returns>The value returned by the corresponding match function.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public ValueTask<TOut> MatchAsync<TOut>(Func<TResult, TOut> onSuccess, Func<TError, ValueTask<TOut>> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => ValueTask.FromResult(onSuccess(ok.Value)),
                Fail<TResult, TError> fail => onFailure(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };
    }

    extension<TResult, TError>(Task<Result<TResult, TError>> resultTask)
    {
        /// <summary>Executes the specified actions based on the result's success or failure, and returns the original result.</summary>
        /// <param name="onSuccess">The action to execute if the result is successful.</param>
        /// <param name="onFailure">The action to execute if the result is a failure.</param>
        /// <returns>The original result after executing the specified actions.</returns>
        public Task<Result<TResult, TError>> Tap(Action<TResult> onSuccess, Action<TError>? onFailure = null) => resultTask.ContinueWith(task => task.Result.Tap(onSuccess, onFailure), TaskContinuationOptions.ExecuteSynchronously);

/// <summary>Executes the specified asynchronous actions based on the result's success or failure, and returns the original result.</summary>
/// <param name="onSuccess">The action to execute if the result is successful.</param>
/// <param name="onFailure">The action to execute if the result is a failure.</param>
/// <returns>The original result after executing the specified actions.</returns>
        public async Task<Result<TResult, TError>> TapAsync(Action<TResult> onSuccess, Action<TError>? onFailure = null)
        {
            var result = await resultTask.ConfigureAwait(false);

            return result.Tap(onSuccess, onFailure);
        }

/// <summary>Executes the specified asynchronous binder function if the result is successful, and returns the resulting mapped result. If the result is a failure, the failure is propagated.</summary>
/// <typeparam name="TMapped">The type of the value in the resulting mapped result.</typeparam>
/// <param name="binder">The asynchronous function to execute if the result is successful.</param>
/// <returns>The resulting mapped result after executing the binder function, or the original failure if the result was a failure.</returns>
        public async Task<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, Task<Result<TMapped, TError>>> binder)
        {
            var result = await resultTask.ConfigureAwait(false);

            return await result.BindAsync(binder).ConfigureAwait(false);
        }

/// <summary>
/// Executes the specified asynchronous binder function (returning a ValueTask) if the result is successful, and returns the resulting mapped result. If the result is a failure, the failure is propagated.
/// </summary>
/// <typeparam name="TMapped">The type of the value in the resulting mapped result.</typeparam>
/// <param name="binder">The asynchronous function (returning a ValueTask) to execute if the result is successful.</param>
/// <returns>The resulting mapped result after executing the binder function, or the original failure if the result was a failure.</returns>
        public async Task<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, ValueTask<Result<TMapped, TError>>> binder)
        {
            var result = await resultTask.ConfigureAwait(false);

            return await result.BindAsync(binder).ConfigureAwait(false);
        }

/// <summary>Executes the specified asynchronous fallback function if the result is a failure, and returns the resulting result. If the result is successful, the success is propagated.</summary>
/// <param name="fallback">The asynchronous fallback function to execute if the result is a failure.</param>
/// <returns>The resulting result after executing the fallback function, or the original success if the result was successful.</returns>
/// <exception cref="InvalidOperationException"></exception>
        public async Task<Result<TResult, TError>> OrElseAsync(Func<TError, Task<Result<TResult, TError>>> fallback)
            => await resultTask.ConfigureAwait(false) switch
            {
                Ok<TResult, TError> ok => ok,
                Fail<TResult, TError> fail => await fallback(fail.Error).ConfigureAwait(false),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

/// <summary>Executes the specified action after the result is obtained, regardless of whether it is successful or a failure. This is useful for performing cleanup or finalization logic.</summary>
/// <param name="finallyAction">The action to execute after the result is obtained, regardless of success or failure.</param>
/// <returns>The original result after executing the finally action.</returns>
        public async Task<Result<TResult, TError>> EnsureAsync(Action finallyAction)
        {
            var result = await resultTask.ConfigureAwait(false);
            finallyAction();

            return result;
        }

/// <summary>
/// Executes the specified asynchronous action after the result is obtained, regardless of whether it is successful or a failure. This is useful for performing cleanup or finalization logic.
/// </summary>
/// <param name="finallyAction">The asynchronous action to execute after the result is obtained, regardless of success or failure.</param>
/// <returns>The original result after executing the finally action.</returns>
        public async Task<Result<TResult, TError>> EnsureAsync(Func<ValueTask> finallyAction)
        {
            var result = await resultTask.ConfigureAwait(false);
            await finallyAction().ConfigureAwait(false);

            return result;
        }

/// <summary>Executes the specified functions based on whether the result is successful or a failure, and returns the corresponding output.</summary>
/// <typeparam name="TOut">The type of the output value.</typeparam>
/// <param name="onSuccess">The function to execute if the result is successful.</param>
/// <param name="onFailure">The function to execute if the result is a failure.</param>
/// <returns>The output value returned by the corresponding function based on the result.</returns>
        public async Task<TOut> MatchAsync<TOut>(Func<TResult, Task<TOut>> onSuccess, Func<TError, Task<TOut>> onFailure)
            => await (await resultTask.ConfigureAwait(false)).MatchAsync(onSuccess, onFailure).ConfigureAwait(false);

/// <summary>Executes the specified functions based on whether the result is successful or a failure, and returns the corresponding output.</summary>
/// <typeparam name="TOut">The type of the output value.</typeparam>
/// <param name="onSuccess">The function to execute if the result is successful.</param>
/// <param name="onFailure">The function to execute if the result is a failure.</param>
/// <returns>The output value returned by the corresponding function based on the result.</returns>
        public async Task<TOut> MatchAsync<TOut>(Func<TResult, TOut> onSuccess, Func<TError, TOut> onFailure)
            => (await resultTask.ConfigureAwait(false)).Match(onSuccess, onFailure);
    }

    extension<TResult, TError>(ValueTask<Result<TResult, TError>> resultTask)
    {
        /// <summary>Executes the specified actions based on whether the result is successful or a failure, and returns the original result.</summary>
        /// <param name="onSuccess">The action to execute if the result is successful.</param>
        /// <param name="onFailure">The action to execute if the result is a failure.</param>
        /// <returns>The original result after executing the specified actions.</returns>
        public async ValueTask<Result<TResult, TError>> TapAsync(Action<TResult> onSuccess, Action<TError>? onFailure = null)
        {
            var result = await resultTask.ConfigureAwait(false);

            return result.Tap(onSuccess, onFailure);
        }

/// <summary>Executes the specified binder function if the result is successful, and returns the resulting mapped result.</summary>
/// <typeparam name="TMapped">The type of the mapped result.</typeparam>
/// <param name="binder">The binder function to execute if the result is successful.</param>
/// <returns>The resulting mapped result after executing the binder function.</returns>
        public async ValueTask<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, Task<Result<TMapped, TError>>> binder)
        {
            var result = await resultTask.ConfigureAwait(false);

            return await result.BindAsync(binder).ConfigureAwait(false);
        }

/// <summary>Executes the specified binder function if the result is successful, and returns the resulting mapped result.</summary>
/// <typeparam name="TMapped">The type of the mapped result.</typeparam>
/// <param name="binder">The binder function to execute if the result is successful.</param>
/// <returns>The resulting mapped result after executing the binder function.</returns>
        public async ValueTask<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, ValueTask<Result<TMapped, TError>>> binder)
        {
            var result = await resultTask.ConfigureAwait(false);

            return await result.BindAsync(binder).ConfigureAwait(false);
        }

/// <summary>Executes the specified action after the result has been processed, regardless of whether it was successful or a failure.</summary>
/// <param name="finallyAction">The action to execute after the result has been processed.</param>
/// <returns>The original result after executing the specified action.</returns>
        public async ValueTask<Result<TResult, TError>> EnsureAsync(Action finallyAction)
        {
            var result = await resultTask.ConfigureAwait(false);
            finallyAction();

            return result;
        }

/// <summary>Executes the specified asynchronous action after the result has been processed, regardless of whether it was successful or a failure.</summary>
/// <param name="finallyAction">The asynchronous action to execute after the result has been processed.</param>
/// <returns>The original result after executing the specified asynchronous action.</returns>
        public async ValueTask<Result<TResult, TError>> EnsureAsync(Func<ValueTask> finallyAction)
        {
            var result = await resultTask.ConfigureAwait(false);
            await finallyAction().ConfigureAwait(false);

            return result;
        }
    }
}
