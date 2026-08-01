using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Guard.Clauses;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

/// <summary>
///     Models the decision shared by the UI button handlers (<c>MainWindow</c>, <c>TagsView</c>,
///     <c>ClassificationsView</c>, <c>ScrapeConfigurationView</c>): the workflow must only run
///     when the preceding setup step (resetting the cancellation token source / disabling controls) succeeded.
///     A failed setup step must short-circuit the chain, and the workflow must never be invoked.
/// </summary>
public static class ScrapeSiteWorkflowDecision
{
    /// <summary>
    ///     Decides whether <paramref name="workflow" /> should run, based on the outcome of the setup step.
    /// </summary>
    /// <param name="setupResult">The outcome of the setup step that must precede the workflow.</param>
    /// <param name="workflow">The workflow to invoke only when <paramref name="setupResult" /> is a success.</param>
    /// <typeparam name="TResult">The success type produced by the workflow.</typeparam>
    /// <returns>
    ///     The result of <paramref name="workflow" /> when <paramref name="setupResult" /> is a success; otherwise a
    ///     failure carrying the setup error, without invoking <paramref name="workflow" />.
    /// </returns>
    public static Task<Result<TResult, string>> DecideAsync<TResult>(Result<CancellationToken, Exception> setupResult, Func<CancellationToken, Task<Result<TResult, string>>> workflow)
    {
        GuardAgainst.Null(setupResult);
        GuardAgainst.Null(workflow);

        return setupResult.MatchAsync(workflow, exception => Result.Failure<TResult, string>(exception.Message)).AsTask();
    }
}
