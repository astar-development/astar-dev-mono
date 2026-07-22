namespace AStar.Dev.Utilities;

/// <summary>
/// </summary>
public static class LinqExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="enumerable"></param>
    /// <param name="action"></param>
    /// <typeparam name="T"></typeparam>
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        if (enumerable == null || action == null) return;

        foreach(var item in enumerable) action(item);
    }

    /// <summary>
    ///     Awaits the supplied asynchronous action once per item, strictly sequentially, so no two invocations
    ///     ever run concurrently.
    /// </summary>
    /// <param name="enumerable"></param>
    /// <param name="action"></param>
    /// <typeparam name="T"></typeparam>
    public static async Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action)
    {
        foreach (var item in enumerable)
            await action(item).ConfigureAwait(false);
    }
}
