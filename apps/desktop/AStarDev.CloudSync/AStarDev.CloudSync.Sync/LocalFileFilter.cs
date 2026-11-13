using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class LocalFileFilter : ILocalFileFilter
{
    /// <inheritdoc />
    public LocalFileVisibility ShouldSkip(IFileInfo file) => throw new NotImplementedException($"{file} is not yet judged.");
}
