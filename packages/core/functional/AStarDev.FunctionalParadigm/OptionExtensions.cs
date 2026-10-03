using AStarDev.FunctionalParadigm;

namespace AStarDev.FunctionalParadigm;

/// <summary>Functional helpers and utilities for working with <see cref="Option{T}" />.</summary>
public static class OptionExtensions
{
    private static readonly string unreachableMessage = "It should not be possible to reach this point.";

    extension<T>(T value)
    {
        /// <summary>Converts a value to an <see cref="Option{T}" />, treating default/null as <c>None</c>.</summary>
        public Option<T> ToOption() => EqualityComparer<T>.Default.Equals(value, default)
                ? Option.None<T>()
                : new Option<T>.Some(value);

        /// <summary>Converts a value to an <see cref="Option{T}" /> if it satisfies the predicate.</summary>
        public Option<T> ToOption(Func<T, bool> predicate) => predicate(value)
                ? new Option<T>.Some(value)
                : Option.None<T>();
    }

    extension<T>(T? nullable) where T : struct
    {
        /// <summary>Converts a nullable value type to an <see cref="Option{T}" />.</summary>
        public Option<T> ToOption() => nullable.HasValue
                ? new Option<T>.Some(nullable.Value)
                : Option.None<T>();
    }

    extension<T>(Option<T> option)
    {
        /// <summary>Attempts to extract the value from an <see cref="Option{T}" />.</summary>
        public bool TryGetValue(out T value)
        {
            if (option is Option<T>.Some some)
            {
                value = some.Value;

                return true;
            }

            value = default!;

            return false;
        }

        /// <summary>Transforms the value inside an <see cref="Option{T}" /> if present.</summary>
        public Option<TResult> Map<TResult>(Func<T, TResult> map)
            => option.Match(some => new Option<TResult>.Some(map(some)), Option.None<TResult>);

        /// <summary>Chains another <see cref="Option{T}" />-producing function.</summary>
        public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> bind)
            => option.Match(bind, Option.None<TResult>);

        /// <summary>Converts an <see cref="Option{T}" /> to a <see cref="Result{T, TError}" />.</summary>
        public Result<T, TError> ToResult<TError>(Func<TError> errorFactory)
            => option.Match(Result.Success<T, TError>, () => Result.Failure<T, TError>(errorFactory()));

        /// <summary>Converts an <see cref="Option{T}" /> to a single-element enumerable or an empty sequence.</summary>
        public IEnumerable<T> ToEnumerable() => option is Option<T>.Some some ? [some.Value] : [];

        /// <summary>Asynchronously transforms the value inside an <see cref="Option{T}" /> if present.</summary>
        public async Task<Option<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapAsync)
            => option switch
            {
                Option<T>.Some some => new Option<TResult>.Some(await mapAsync(some.Value)),
                Option<T>.None => Option.None<TResult>(),
                _ => throw new InvalidOperationException(unreachableMessage)
            };

        /// <summary>Asynchronously chains another <see cref="Option{T}" />-producing function.</summary>
        public async Task<Option<TResult>> BindAsync<TResult>(Func<T, Task<Option<TResult>>> bindAsync)
            => option switch
            {
                Option<T>.Some some => await bindAsync(some.Value),
                Option<T>.None => Option.None<TResult>(),
                _ => throw new InvalidOperationException(unreachableMessage)
            };

        /// <summary>Asynchronously converts an <see cref="Option{T}" /> to a <see cref="Result{T, TError}" />.</summary>
        public async Task<Result<T, TError>> ToResultAsync<TError>(Func<Task<TError>> errorFactoryAsync)
            => await option.Match(
                                some => Task.FromResult(Result.Success<T, TError>(some)),
                                async () => Result.Failure<T, TError>(await errorFactoryAsync()));

        /// <summary>Executes a side-effect action on the value if present, and returns the original option.</summary>
        public Option<T> Tap(Action<T> action)
        {
            if (option is Option<T>.Some some) action(some.Value);

            return option;
        }

        /// <summary>Asynchronously executes a side-effect action on the value if present, and returns the original option.</summary>
        public async Task<Option<T>> TapAsync(Func<T, Task> actionAsync)
        {
            if (option is Option<T>.Some some) await actionAsync(some.Value);

            return option;
        }

        /// <summary>Pattern matches on the option with an asynchronous function for Some.</summary>
        public async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSomeAsync, Func<TResult> onNone)
            => option switch
            {
                Option<T>.Some some => await onSomeAsync(some.Value),
                Option<T>.None => onNone(),
                _ => throw new InvalidOperationException(unreachableMessage)
            };

        /// <summary>Pattern matches on the option with an asynchronous function for None.</summary>
        public async Task<TResult> MatchAsync<TResult>(Func<T, TResult> onSome, Func<Task<TResult>> onNoneAsync)
            => option switch
            {
                Option<T>.Some some => onSome(some.Value),
                Option<T>.None => await onNoneAsync(),
                _ => throw new InvalidOperationException(unreachableMessage)
            };

        /// <summary>Pattern matches on the option with asynchronous functions for both Some and None.</summary>
        public async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSomeAsync, Func<Task<TResult>> onNoneAsync)
            => option switch
            {
                Option<T>.Some some => await onSomeAsync(some.Value),
                Option<T>.None => await onNoneAsync(),
                _ => throw new InvalidOperationException(unreachableMessage)
            };

        /// <summary>Pattern matches on the option for side effects, awaiting the matched branch.</summary>
        public async Task MatchAsync(Func<T, Task> onSomeAsync, Action onNone)
        {
            switch (option)
            {
                case Option<T>.Some some:
                    await onSomeAsync(some.Value);
                    break;

                case Option<T>.None:
                    onNone();
                    break;

                default:
                    throw new InvalidOperationException(unreachableMessage);
            }
        }

        /// <summary>Filters an option by a predicate, turning Some values that don't satisfy the predicate into None.</summary>
        public Option<T> Filter(Func<T, bool> predicate)
            => option.Match(
                         some => predicate(some) ? option : Option.None<T>(),
                         Option.None<T>);

        /// <summary>Maps the value if present, or returns a default value.</summary>
        public TResult MapOrDefault<TResult>(Func<T, TResult> map, TResult defaultValue)
            => option.Match(map, () => defaultValue);

        /// <summary>Maps the value if present, or computes a default value.</summary>
        public TResult MapOrElse<TResult>(Func<T, TResult> map, Func<TResult> defaultFactory)
            => option.Match(map, defaultFactory);
    }

    extension<T>(Option<T> option) where T : struct
    {
        /// <summary>Converts an <see cref="Option{T}" /> to a nullable type.</summary>
        public T? ToNullable() => option is Option<T>.Some some ? some.Value : null;
    }

    extension<T>(Task<Option<T>> optionTask)
    {
        /// <summary>Awaits the <see cref="Option{T}" />-producing task, then pattern matches on the result.</summary>
        public async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSome, Func<TResult> onNone)
        {
            var option = await optionTask;

            return await option.MatchAsync(onSome, onNone);
        }

        /// <summary>Awaits the <see cref="Option{T}" />-producing task, then pattern matches on the result for side effects.</summary>
        public async Task MatchAsync(Func<T, Task> onSome, Action onNone)
        {
            var option = await optionTask;

            await option.MatchAsync(onSome, onNone);
        }

        /// <summary>Asynchronously transforms the value inside a Task of <see cref="Option{T}" /> if present.</summary>
        public async Task<Option<TResult>> MapAsync<TResult>(Func<T, TResult> map)
            => (await optionTask).Map(map);

        /// <summary>Asynchronously transforms the value inside a Task of <see cref="Option{T}" /> if present.</summary>
        public async Task<Option<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapAsync)
        {
            var option = await optionTask;

            return await option.MapAsync(mapAsync);
        }

        /// <summary>Asynchronously chains another <see cref="Option{T}" />-producing function.</summary>
        public async Task<Option<TResult>> BindAsync<TResult>(Func<T, Option<TResult>> bind)
            => (await optionTask).Bind(bind);

        /// <summary>Asynchronously chains another <see cref="Option{T}" />-producing function.</summary>
        public async Task<Option<TResult>> BindAsync<TResult>(Func<T, Task<Option<TResult>>> bindAsync)
        {
            var option = await optionTask;

            return await option.BindAsync(bindAsync);
        }

        /// <summary>Asynchronously converts a Task of <see cref="Option{T}" /> to a <see cref="Result{T, TError}" />.</summary>
        public async Task<Result<T, TError>> ToResultAsync<TError>(Func<TError> errorFactory) =>
            (await optionTask).ToResult(errorFactory);

        /// <summary>Asynchronously converts a Task of <see cref="Option{T}" /> to a <see cref="Result{T, TError}" />.</summary>
        public async Task<Result<T, TError>> ToResultAsync<TError>(Func<Task<TError>> errorFactoryAsync)
        {
            var option = await optionTask;

            return await option.ToResultAsync(errorFactoryAsync);
        }

        /// <summary>Executes a side-effect action on the value if present, and returns the original option.</summary>
        public async Task<Option<T>> TapAsync(Action<T> action)
        {
            var option = await optionTask;

            return option.Tap(action);
        }

        /// <summary>Asynchronously executes a side-effect action on the value if present, and returns the original option.</summary>
        public async Task<Option<T>> TapAsync(Func<T, Task> actionAsync)
        {
            var option = await optionTask;

            return await option.TapAsync(actionAsync);
        }
    }

    extension<T>(IEnumerable<Option<T>> options)
    {
        /// <summary>Filters out None values and unwraps Some values into a new sequence.</summary>
        public IEnumerable<T> Values()
        {
            foreach (var option in options)
                if (option is Option<T>.Some some) yield return some.Value;
        }
    }

    extension<T>(IEnumerable<T> source)
    {
        /// <summary>Transforms a sequence by keeping only elements that match the predicate and wrapping them in Options.</summary>
        public IEnumerable<Option<T>> Choose(Func<T, bool> predicate)
            => from item in source where predicate(item) select new Option<T>.Some(item);

        /// <summary>Transforms a sequence by applying a mapping function that returns Options and keeping only valid Some results.</summary>
        public IEnumerable<TResult> Choose<TResult>(Func<T, Option<TResult>> chooser)
        {
            foreach (var item in source)
            {
                var option = chooser(item);

                if (option is Option<TResult>.Some some) yield return some.Value;
            }
        }
    }
}
