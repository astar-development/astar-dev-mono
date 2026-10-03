namespace AStarDev.FunctionalParadigm;

/// <summary>LINQ-style query support for <see cref="Option{T}" />.</summary>
public static class OptionLinqExtensions
{
    extension<T>(Option<T> option)
    {
        /// <summary>Projects the value of a <see cref="Option{T}" /> using the specified function.</summary>
        public Option<TResult> Select<TResult>(Func<T, TResult> selector) => option.Map(selector);

        /// <summary>Projects and flattens nested <see cref="Option{T}" /> structures using a LINQ-style binding function.</summary>
        public Option<TResult> SelectMany<TIntermediate, TResult>(Func<T, Option<TIntermediate>> bind, Func<T, TIntermediate, TResult> project) =>
            option.Bind(x => bind(x).Map(y => project(x, y)));
    }

    extension<T>(Task<Option<T>> task)
    {
        /// <summary>Asynchronously projects the value of a <see cref="Task{Option}" /> using the specified function.</summary>
        public async Task<Option<TResult>> SelectAwaitAsync<TResult>(Func<T, Task<TResult>> selector)
        {
            var option = await task;

            return option is Option<T>.Some some
                       ? new Option<TResult>.Some(await selector(some.Value))
                       : Option.None<TResult>();
        }
    }
}
