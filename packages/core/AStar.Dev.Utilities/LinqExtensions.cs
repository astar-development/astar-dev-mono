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
        foreach(var item in enumerable)
        {
            action(item);
        }
    }
}