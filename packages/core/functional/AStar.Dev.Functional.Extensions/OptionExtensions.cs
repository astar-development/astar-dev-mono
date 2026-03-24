namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Functional helpers and utilities for working with <see cref="Option{T}" />.
/// </summary>
public static class OptionExtensions
{
    private static readonly string UnreachableMessage = "It should not be possible to reach this point.";

    /// <summary>
    ///     Attempts to extract the value from an <see cref="Option{T}" />.
    /// </summary>
    public static bool TryGetValue<T>(this Option<T> option, out T value)
    {
        if(option is Option<T>.Some some)
        {
            value = some.Value;

            return true;
        }

        value = default!;

        return false;
    }

    /// <summary>
    ///     Converts a value to an <see cref="Option{T}" />, treating default/null as <c>None</c>.
    /// </summary>
    public static Option<T> ToOption<T>(this T value) =>
        EqualityComparer<T>.Default.Equals(value, default!)
            ? Option.None<T>()
            : new Option<T>.Some(value);

    /// <summary>
    ///     Converts a value to an <see cref="Option{T}" /> if it satisfies the predicate.
    /// </summary>
    public static Option<T> ToOption<T>(this T value, Func<T, bool> predicate) =>
        predicate(value)
            ? new Option<T>.Some(value)
            : Option.None<T>();

    /// <summary>
    ///     Converts a nullable value type to an <see cref="Option{T}" />.
    /// </summary>
    public static Option<T> ToOption<T>(this T? nullable) where T : struct =>
        nullable.HasValue
            ? new Option<T>.Some(nullable.Value)
            : Option.None<T>();

    /// <summary>
    ///     Transforms the value inside an <see cref="Option{T}" /> if present.
    /// </summary>
    public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> map) =>
        option.Match(
                     some => new Option<TResult>.Some(map(some)),
                     Option.None<TResult>);

    /// <summary>
    ///     Chains another <see cref="Option{T}" />-producing function.
    /// </summary>
    public static Option<TResult> Bind<T, TResult>(this Option<T> option, Func<T, Option<TResult>> bind) => option.Match(bind, Option.None<TResult>);

    /// <summary>
    ///     Converts an <see cref="Option{T}" /> to a <see cref="Result{T, TError}" />.
    /// </summary>
    public static Result<T, TError> ToResult<T, TError>(this Option<T> option, Func<TError> errorFactory) =>
        option.Match<Result<T, TError>>(
                                        some => new Result<T, TError>.Ok(some),
                                        () => new Result<T, TError>.Error(errorFactory()));

    /// <summary>
    ///     Determines whether the option contains a value.
    /// </summary>
    public static bool IsSome<T>(this Option<T> option) => option is Option<T>.Some;

    /// <summary>
    ///     Determines whether the option is empty.
    /// </summary>
    public static bool IsNone<T>(this Option<T> option) => option is Option<T>.None;

    /// <summary>
    ///     Converts an <see cref="Option{T}" /> to a nullable type.
    /// </summary>
    public static T? ToNullable<T>(this Option<T> option) where T : struct => option is Option<T>.Some some ? some.Value : null;

    /// <summary>
    ///     Converts an <see cref="Option{T}" /> to a single-element enumerable or an empty sequence.
    /// </summary>
    public static IEnumerable<T> ToEnumerable<T>(this Option<T> option) => option is Option<T>.Some some ? [some.Value] : [];

    /// <summary>
    ///     Gets the value of the option or returns a fallback value.
    /// </summary>
    public static T OrElse<T>(this Option<T> option, T fallback) => option is Option<T>.Some some ? some.Value : fallback;

    /// <summary>
    ///     Gets the value of the option or throws an exception if absent.
    /// </summary>
    public static T OrThrow<T>(this Option<T> option, Exception? ex = null) => option is Option<T>.Some some ? some.Value : throw ex ?? new InvalidOperationException("No value present");

    /// <summary>
    ///     Enables deconstruction of an option into a boolean and value pair.
    /// </summary>
    /// <param name="option"></param>
    /// <param name="isSome"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public static void Deconstruct<T>(this Option<T> option, out bool isSome, out T? value)
    {
        isSome = option is Option<T>.Some;
        value  = isSome ? ((Option<T>.Some)option).Value : default;
    }

    /// <summary>
    ///     Asynchronously transforms the value inside an <see cref="Option{T}" /> if present.
    /// </summary>
    public static async Task<Option<TResult>> MapAsync<T, TResult>(
        this Option<T>         option,
        Func<T, Task<TResult>> mapAsync) =>
        option switch
        {
            Option<T>.Some some => new Option<TResult>.Some(await mapAsync(some.Value)),
            Option<T>.None      => Option.None<TResult>(),
            _                   => throw new InvalidOperationException(UnreachableMessage)
        };

    /// <summary>
    ///     Asynchronously transforms the value inside a Task of <see cref="Option{T}" /> if present.
    /// </summary>
    public static async Task<Option<TResult>> MapAsync<T, TResult>(
        this Task<Option<T>> optionTask,
        Func<T, TResult>     map) =>
        (await optionTask).Map(map);

    /// <summary>
    ///     Asynchronously transforms the value inside a Task of <see cref="Option{T}" /> if present.
    /// </summary>
    public static async Task<Option<TResult>> MapAsync<T, TResult>(
        this Task<Option<T>>   optionTask,
        Func<T, Task<TResult>> mapAsync)
    {
        Option<T> option = await optionTask;

        return await option.MapAsync(mapAsync);
    }

    /// <summary>
    ///     Asynchronously chains another <see cref="Option{T}" />-producing function.
    /// </summary>
    public static async Task<Option<TResult>> BindAsync<T, TResult>(
        this Option<T>                 option,
        Func<T, Task<Option<TResult>>> bindAsync) =>
        option switch
        {
            Option<T>.Some some => await bindAsync(some.Value),
            Option<T>.None      => Option.None<TResult>(),
            _                   => throw new InvalidOperationException(UnreachableMessage)
        };

    /// <summary>
    ///     Asynchronously chains another <see cref="Option{T}" />-producing function.
    /// </summary>
    public static async Task<Option<TResult>> BindAsync<T, TResult>(
        this Task<Option<T>>     optionTask,
        Func<T, Option<TResult>> bind) =>
        (await optionTask).Bind(bind);

    /// <summary>
    ///     Asynchronously chains another <see cref="Option{T}" />-producing function.
    /// </summary>
    public static async Task<Option<TResult>> BindAsync<T, TResult>(
        this Task<Option<T>>           optionTask,
        Func<T, Task<Option<TResult>>> bindAsync)
    {
        Option<T> option = await optionTask;

        return await option.BindAsync(bindAsync);
    }

    /// <summary>
    ///     Asynchronously converts an <see cref="Option{T}" /> to a <see cref="Result{T, TError}" />.
    /// </summary>
    public static async Task<Result<T, TError>> ToResultAsync<T, TError>(
        this Option<T>     option,
        Func<Task<TError>> errorFactoryAsync) =>
        await option.Match<Task<Result<T, TError>>>(
                                                    some => Task.FromResult<Result<T, TError>>(new Result<T, TError>.Ok(some)),
                                                    async () => new Result<T, TError>.Error(await errorFactoryAsync()));

    /// <summary>
    ///     Asynchronously converts a Task of <see cref="Option{T}" /> to a <see cref="Result{T, TError}" />.
    /// </summary>
    public static async Task<Result<T, TError>> ToResultAsync<T, TError>(
        this Task<Option<T>> optionTask,
        Func<TError>         errorFactory) =>
        (await optionTask).ToResult(errorFactory);

    /// <summary>
    ///     Asynchronously converts a Task of <see cref="Option{T}" /> to a <see cref="Result{T, TError}" />.
    /// </summary>
    public static async Task<Result<T, TError>> ToResultAsync<T, TError>(
        this Task<Option<T>> optionTask,
        Func<Task<TError>>   errorFactoryAsync)
    {
        Option<T> option = await optionTask;

        return await option.ToResultAsync(errorFactoryAsync);
    }

    /// <summary>
    ///     Executes a side-effect action on the value if present, and returns the original option.
    /// </summary>
    public static Option<T> Tap<T>(this Option<T> option, Action<T> action)
    {
        if(option is Option<T>.Some some)
        {
            action(some.Value);
        }

        return option;
    }

    /// <summary>
    ///     Asynchronously executes a side-effect action on the value if present, and returns the original option.
    /// </summary>
    public static async Task<Option<T>> TapAsync<T>(
        this Option<T> option,
        Func<T, Task>  actionAsync)
    {
        if(option is Option<T>.Some some)
        {
            await actionAsync(some.Value);
        }

        return option;
    }

    /// <summary>
    ///     Executes a side-effect action on the value if present, and returns the original option.
    /// </summary>
    public static async Task<Option<T>> TapAsync<T>(
        this Task<Option<T>> optionTask,
        Action<T>            action)
    {
        Option<T> option = await optionTask;

        return option.Tap(action);
    }

    /// <summary>
    ///     Asynchronously executes a side-effect action on the value if present, and returns the original option.
    /// </summary>
    public static async Task<Option<T>> TapAsync<T>(
        this Task<Option<T>> optionTask,
        Func<T, Task>        actionAsync)
    {
        Option<T> option = await optionTask;

        return await option.TapAsync(actionAsync);
    }

    /// <summary>
    ///     Pattern matches on the option with an asynchronous function for Some.
    /// </summary>
    public static async Task<TResult> MatchAsync<T, TResult>(
        this Option<T>         option,
        Func<T, Task<TResult>> onSomeAsync,
        Func<TResult>          onNone) =>
        option switch
        {
            Option<T>.Some some => await onSomeAsync(some.Value),
            Option<T>.None      => onNone(),
            _                   => throw new InvalidOperationException(UnreachableMessage)
        };

    /// <summary>
    ///     Pattern matches on the option with an asynchronous function for None.
    /// </summary>
    public static async Task<TResult> MatchAsync<T, TResult>(
        this Option<T>      option,
        Func<T, TResult>    onSome,
        Func<Task<TResult>> onNoneAsync) =>
        option switch
        {
            Option<T>.Some some => onSome(some.Value),
            Option<T>.None      => await onNoneAsync(),
            _                   => throw new InvalidOperationException(UnreachableMessage)
        };

    /// <summary>
    ///     Pattern matches on the option with asynchronous functions for both Some and None.
    /// </summary>
    public static async Task<TResult> MatchAsync<T, TResult>(
        this Option<T>         option,
        Func<T, Task<TResult>> onSomeAsync,
        Func<Task<TResult>>    onNoneAsync) =>
        option switch
        {
            Option<T>.Some some => await onSomeAsync(some.Value),
            Option<T>.None      => await onNoneAsync(),
            _                   => throw new InvalidOperationException(UnreachableMessage)
        };

    /// <summary>
    ///     Gets the value of the option or returns a fallback value from an async function.
    /// </summary>
    public static async Task<T> OrElseAsync<T>(
        this Option<T> option,
        Func<Task<T>>  getFallbackAsync) =>
        option is Option<T>.Some some ? some.Value : await getFallbackAsync();

    /// <summary>
    ///     Gets the value of a Task of Option or returns a fallback value.
    /// </summary>
    public static async Task<T> OrElseAsync<T>(
        this Task<Option<T>> optionTask,
        T                    fallback)
    {
        Option<T> option = await optionTask;

        return option.OrElse(fallback);
    }

    /// <summary>
    ///     Gets the value of a Task of Option or returns a fallback value from an async function.
    /// </summary>
    public static async Task<T> OrElseAsync<T>(
        this Task<Option<T>> optionTask,
        Func<Task<T>>        getFallbackAsync)
    {
        Option<T> option = await optionTask;

        return await option.OrElseAsync(getFallbackAsync);
    }

    /// <summary>
    ///     Filters out None values and unwraps Some values into a new sequence.
    /// </summary>
    public static IEnumerable<T> Values<T>(this IEnumerable<Option<T>> options)
    {
        foreach(Option<T>? option in options)
        {
            if(option is Option<T>.Some some)
            {
                yield return some.Value;
            }
        }
    }

    /// <summary>
    ///     Transforms a sequence by keeping only elements that match the predicate
    ///     and wrapping them in Options.
    /// </summary>
    public static IEnumerable<Option<T>> Choose<T>(
        this IEnumerable<T> source,
        Func<T, bool>       predicate) =>
        from item in source where predicate(item) select new Option<T>.Some(item);

    /// <summary>
    ///     Transforms a sequence by applying a mapping function that returns Options
    ///     and keeping only valid Some results.
    /// </summary>
    public static IEnumerable<TResult> Choose<T, TResult>(
        this IEnumerable<T>      source,
        Func<T, Option<TResult>> chooser)
    {
        foreach(T item in source)
        {
            Option<TResult>? option = chooser(item);

            if(option is Option<TResult>.Some some)
            {
                yield return some.Value;
            }
        }
    }

    /// <summary>
    ///     Filters an option by a predicate, turning Some values that don't satisfy the predicate into None.
    /// </summary>
    public static Option<T> Filter<T>(this Option<T> option, Func<T, bool> predicate) =>
        option.Match(
                     some => predicate(some) ? option : Option.None<T>(),
                     () => Option.None<T>());

    /// <summary>
    ///     Maps the value if present, or returns a default value.
    /// </summary>
    public static TResult MapOrDefault<T, TResult>(
        this Option<T>   option,
        Func<T, TResult> map,
        TResult          defaultValue) =>
        option.Match(
                     some => map(some),
                     () => defaultValue);

    /// <summary>
    ///     Maps the value if present, or computes a default value.
    /// </summary>
    public static TResult MapOrElse<T, TResult>(
        this Option<T>   option,
        Func<T, TResult> map,
        Func<TResult>    defaultFactory) =>
        option.Match(
                     some => map(some),
                     defaultFactory);
}
