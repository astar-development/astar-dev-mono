namespace AStarDev.WallpaperScraper.Scoping;

/// <summary>Runs work against scoped services inside a dedicated dependency injection scope, so singletons need not hold a service locator.</summary>
public interface IScopedRunner
{
    /// <summary>Runs <paramref name="work"/> with <typeparamref name="TService"/> resolved from a new scope that is disposed afterwards.</summary>
    /// <typeparam name="TService">The scoped service to resolve.</typeparam>
    /// <typeparam name="TResult">The result type of the work.</typeparam>
    /// <param name="work">The work to run.</param>
    /// <returns>The result of <paramref name="work"/>.</returns>
    Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> work)
        where TService : notnull;

    /// <summary>Runs <paramref name="work"/> with both services resolved from the same new scope that is disposed afterwards.</summary>
    /// <typeparam name="TFirst">The first scoped service to resolve.</typeparam>
    /// <typeparam name="TSecond">The second scoped service to resolve.</typeparam>
    /// <typeparam name="TResult">The result type of the work.</typeparam>
    /// <param name="work">The work to run.</param>
    /// <returns>The result of <paramref name="work"/>.</returns>
    Task<TResult> RunAsync<TFirst, TSecond, TResult>(Func<TFirst, TSecond, Task<TResult>> work)
        where TFirst : notnull
        where TSecond : notnull;

    /// <summary>Runs <paramref name="work"/> with all three services resolved from the same new scope that is disposed afterwards.</summary>
    /// <typeparam name="TFirst">The first scoped service to resolve.</typeparam>
    /// <typeparam name="TSecond">The second scoped service to resolve.</typeparam>
    /// <typeparam name="TThird">The third scoped service to resolve.</typeparam>
    /// <typeparam name="TResult">The result type of the work.</typeparam>
    /// <param name="work">The work to run.</param>
    /// <returns>The result of <paramref name="work"/>.</returns>
    Task<TResult> RunAsync<TFirst, TSecond, TThird, TResult>(Func<TFirst, TSecond, TThird, Task<TResult>> work)
        where TFirst : notnull
        where TSecond : notnull
        where TThird : notnull;
}
