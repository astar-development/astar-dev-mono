namespace AStarDev.CloudSync.Classification;

/// <summary>One run's link to the <see cref="IReclassifyProgressSource"/>: pass it as the run's progress, and dispose it when the run ends, however it ends. A scope that never reported, such as a refused run, leaves the source untouched when disposed.</summary>
public interface IReclassifyRunScope : IProgress<ReclassifyProgress>, IDisposable
{
}
