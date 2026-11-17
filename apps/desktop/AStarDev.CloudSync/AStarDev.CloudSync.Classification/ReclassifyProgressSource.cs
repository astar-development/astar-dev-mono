using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Keeps the latest report of the active reclassify run. Safe to call from any thread.</summary>
public sealed class ReclassifyProgressSource : IReclassifyProgressSource
{
    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public Option<ReclassifyProgress> Current => throw new NotImplementedException($"{nameof(Current)} {Changed}");

    /// <summary>Starts tracking one run.</summary>
    /// <param name="downstream">Receives every report after the source has recorded it.</param>
    /// <returns>The scope to pass as the run's progress and to dispose when the run ends.</returns>
    public IReclassifyRunScope Begin(IProgress<ReclassifyProgress> downstream) => throw new NotImplementedException($"{nameof(Begin)} {downstream}");
}
