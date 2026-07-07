using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>The production <see cref="IImageSaver" />, backed by an injected <see cref="IFileSystem" />, frozen from the historical <c>ImageSaveHelper</c> behaviour.</summary>
public sealed class ImageSaver(IFileSystem fileSystem) : IImageSaver
{
    /// <inheritdoc />
    public async Task<Result<Unit, ScrapeError>> SaveAsync(byte[] image, string path)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(path);

        string cleanedPath = path.CleanPath();

        if (cleanedPath.LastIndexOf(':') > 2) cleanedPath = cleanedPath[..2] + cleanedPath[2..].Replace(":", "_");

        return (await Try.RunAsync(() => WriteImageAsync(cleanedPath, image)).ConfigureAwait(false))
            .ToResult<Unit, ScrapeError>(exception => ScrapeErrorFactory.CreateImageSaveFailed(path, exception.Message));
    }

    private async Task<Unit> WriteImageAsync(string cleanedPath, byte[] image)
    {
        if (image.Length > 0) await fileSystem.File.WriteAllBytesAsync(cleanedPath, image).ConfigureAwait(false);

        return Unit.Value;
    }
}
