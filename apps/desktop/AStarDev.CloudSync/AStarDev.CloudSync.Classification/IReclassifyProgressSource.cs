using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Shows the progress of whichever reclassify run is active, whoever started it, so a page opened mid-run can show it (#1906). The latest report is kept, so a late subscriber sees it at once. <see cref="Changed"/> is raised on the thread that reported, which is rarely the UI thread; a listener must unsubscribe when it is done.</summary>
public interface IReclassifyProgressSource
{
    /// <summary>Raised after <see cref="Current"/> changed: a run reported progress or finished.</summary>
    event EventHandler Changed;

    /// <summary>The latest report of the active run, or none when no run has reported yet or the last one ended. A run reports after its first batch, so the first moments of a run show nothing.</summary>
    Option<ReclassifyProgress> Current { get; }
}
