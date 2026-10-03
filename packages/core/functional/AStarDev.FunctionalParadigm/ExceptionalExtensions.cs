using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace AStarDev.FunctionalParadigm;

/// <summary>Functional helpers and utilities for working with <see cref="Exceptional{T}" />.</summary>
public static class ExceptionalExtensions
{
    private const string UnexpectedExceptionalTypeMessage = "Unexpected exceptional type.";

    [DoesNotReturn]
    private static TOut Rethrow<TOut>(Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();

        throw exception;
    }

    extension<T>(Exceptional<T> exceptional)
    {
        /// <summary>Pattern matches on the <see cref="Exceptional{T}" />, invoking the handler for the case present.</summary>
        public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Exception, TOut> onFailure)
            => exceptional switch
            {
                Success<T> success => onSuccess(success.Value),
                Failure<T> failure => onFailure(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage + $" Type: {exceptional.GetType().FullName}")
            };

        /// <summary>Returns the value of a <see cref="Success{T}" />, or throws the captured exception of a <see cref="Failure{T}" />.</summary>
        public T GetOrThrow()
            => exceptional.Match(value => value, Rethrow<T>);

        /// <summary>Asynchronously pattern matches on the <see cref="Exceptional{T}" />, invoking the async success handler.</summary>
        public async Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> onSuccess, Func<Exception, TOut> onFailure)
            => exceptional switch
            {
                Success<T> success => await onSuccess(success.Value).ConfigureAwait(false),
                Failure<T> failure => onFailure(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };

        /// <summary>Transforms the value inside a <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.</summary>
        public Exceptional<TResult> Map<TResult>(Func<T, TResult> selector)
            => exceptional switch
            {
                Success<T> success => new Success<TResult>(selector(success.Value)),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };

        /// <summary>Asynchronously transforms the value inside a <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.</summary>
        public async Task<Exceptional<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> selector)
            => exceptional switch
            {
                Success<T> success => new Success<TResult>(await selector(success.Value).ConfigureAwait(false)),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };

        /// <summary>Asynchronously transforms the value inside a <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.</summary>
        public async ValueTask<Exceptional<TResult>> MapAsync<TResult>(Func<T, ValueTask<TResult>> selector)
            => exceptional switch
            {
                Success<T> success => new Success<TResult>(await selector(success.Value).ConfigureAwait(false)),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };

        /// <summary>Chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.</summary>
        public Exceptional<TResult> Bind<TResult>(Func<T, Exceptional<TResult>> binder)
            => exceptional switch
            {
                Success<T> success => binder(success.Value),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };

        /// <summary>Asynchronously chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.</summary>
        public async Task<Exceptional<TResult>> BindAsync<TResult>(Func<T, Task<Exceptional<TResult>>> binder)
            => exceptional switch
            {
                Success<T> success => await binder(success.Value).ConfigureAwait(false),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };

        /// <summary>Executes a side-effect action for the case present, and returns the original <see cref="Exceptional{T}" />.</summary>
        public Exceptional<T> Tap(Action<T> onSuccess, Action<Exception>? onFailure = null)
        {
            switch (exceptional)
            {
                case Success<T> success:
                    onSuccess(success.Value);

                    return success;

                case Failure<T> failure:
                    onFailure?.Invoke(failure.Exception);

                    return failure;

                default:
                    throw new InvalidOperationException(UnexpectedExceptionalTypeMessage);
            }
        }

        /// <summary>Executes a side-effect action on the captured exception of an <see cref="Exceptional{T}" />, returning the original result unchanged.</summary>
        public Exceptional<T> TapError(Action<Exception> onFailure)
        {
            if (exceptional is Failure<T> failure) onFailure(failure.Exception);

            return exceptional;
        }

        /// <summary>Lifts an <see cref="Exceptional{T}" /> into a <see cref="Result{TResult,TError}" />, mapping a captured exception to a domain error via <paramref name="mapError" />.</summary>
        public Result<T, TError> ToResult<TError>(Func<Exception, TError> mapError)
            => exceptional switch
            {
                Success<T> success => new Ok<T, TError>(success.Value),
                Failure<T> failure => new Fail<T, TError>(mapError(failure.Exception)),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };
    }

    extension<T>(Task<Exceptional<T>> exceptionalTask)
    {
        /// <summary>Asynchronously pattern matches on a Task of <see cref="Exceptional{T}" />, invoking the handler for the case present.</summary>
        public async Task<TOut> MatchAsync<TOut>(Func<T, TOut> onSuccess, Func<Exception, TOut> onFailure)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return exceptional.Match(onSuccess, onFailure);
        }

        /// <summary>Asynchronously pattern matches on a Task of <see cref="Exceptional{T}" /> for side effects.</summary>
        public async Task MatchAsync(Func<T, Task> onSuccess, Func<Exception, T> onFailure)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            switch (exceptional)
            {
                case Success<T> success:
                    await onSuccess(success.Value).ConfigureAwait(false);

                    break;

                case Failure<T> failure:
                    _ = onFailure(failure.Exception);

                    break;

                default:
                    throw new InvalidOperationException(UnexpectedExceptionalTypeMessage + $" Type: {exceptional.GetType().FullName}");
            }
        }

        /// <summary>Asynchronously transforms the value inside a Task of <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.</summary>
        public async Task<Exceptional<TResult>> MapAsync<TResult>(Func<T, TResult> selector)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return exceptional.Map(selector);
        }

        /// <summary>Asynchronously transforms the value inside a Task of <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />, via an asynchronous selector.</summary>
        public async Task<Exceptional<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> selector)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return await exceptional.MapAsync(selector).ConfigureAwait(false);
        }

        /// <summary>Asynchronously chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.</summary>
        public async Task<Exceptional<TResult>> BindAsync<TResult>(Func<T, Task<Exceptional<TResult>>> binder)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return await exceptional.BindAsync(binder).ConfigureAwait(false);
        }

        /// <summary>Asynchronously chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.</summary>
        public async Task<Exceptional<TResult>> BindAsync<TResult>(Func<T, ValueTask<Exceptional<TResult>>> binder)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return exceptional switch
            {
                Success<T> success => await binder(success.Value).ConfigureAwait(false),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };
        }

        /// <summary>Asynchronously executes a side-effect action for the case present, and returns the original <see cref="Exceptional{T}" />.</summary>
        public async Task<Exceptional<T>> TapAsync(Action<T> onSuccess, Action<Exception>? onFailure = null)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return exceptional.Tap(onSuccess, onFailure);
        }

        /// <summary>Asynchronously executes a side-effect action on the captured exception of a Task of <see cref="Exceptional{T}" />, returning the original result unchanged.</summary>
        public async Task<Exceptional<T>> TapErrorAsync(Action<Exception> onFailure)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return exceptional.TapError(onFailure);
        }

        /// <summary>
        ///     Asynchronously runs a finalizer against a Task of <see cref="Exceptional{T}" /> regardless of whether it
        ///     resolves to a <see cref="Success{T}" /> or a <see cref="Failure{T}" />, and returns the original result.
        /// </summary>
        public async Task<Exceptional<T>> EnsureAsync(Action<T> finallyAction)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            finallyAction(exceptional is Success<T> success ? success.Value : default!);

            return exceptional;
        }

        /// <summary>
        ///     Lifts a Task of <see cref="Exceptional{T}" /> into a <see cref="Result{TResult,TError}" />, mapping a
        ///     captured exception to a domain error via <paramref name="mapError" />.
        /// </summary>
        public async Task<Result<T, TError>> ToResultAsync<TError>(Func<Exception, TError> mapError)
            => (await exceptionalTask.ConfigureAwait(false)).ToResult(mapError);
    }

    extension<T>(ValueTask<Exceptional<T>> exceptionalTask)
    {
        /// <summary>Asynchronously chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.</summary>
        public async ValueTask<Exceptional<TResult>> BindAsync<TResult>(Func<T, Task<Exceptional<TResult>>> binder)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return exceptional switch
            {
                Success<T> success => await binder(success.Value).ConfigureAwait(false),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };
        }

        /// <summary>Asynchronously chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.</summary>
        public async ValueTask<Exceptional<TResult>> BindAsync<TResult>(Func<T, ValueTask<Exceptional<TResult>>> binder)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return exceptional switch
            {
                Success<T> success => await binder(success.Value).ConfigureAwait(false),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException(UnexpectedExceptionalTypeMessage)
            };
        }

        /// <summary>Asynchronously executes a side-effect action for the case present, and returns the original <see cref="Exceptional{T}" />.</summary>
        public async ValueTask<Exceptional<T>> TapAsync(Action<T> onSuccess, Action<Exception>? onFailure = null)
        {
            var exceptional = await exceptionalTask.ConfigureAwait(false);

            return exceptional.Tap(onSuccess, onFailure);
        }
    }
}
