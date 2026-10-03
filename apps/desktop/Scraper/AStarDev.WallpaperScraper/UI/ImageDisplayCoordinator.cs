using System.Diagnostics.CodeAnalysis;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.WallpaperIngestion;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>
/// Bridges <see cref="IImageDownloadNotifier"/> to a display-ready preview: decodes each downloaded
/// wallpaper's image at preview size and raises <see cref="ImageReady"/> with the result. Decoding happens off
/// the notifying (ingestion) thread, is skipped while <see cref="IsEnabled"/> is <c>false</c> (only the details are raised, through <see cref="DetailsReady"/>), and when
/// downloads outpace decoding only the most recent waiting image is decoded. A decode failure is swallowed - a
/// missing preview for one wallpaper isn't worth interrupting the scrape over.
/// </summary>
public sealed class ImageDisplayCoordinator
{
    /// <summary>The maximum width or height, in pixels, a preview image is decoded to.</summary>
    public const int PreviewMaximumDimension = 1280;

    private readonly IDownloadedImageDecoder decoder;
    private readonly Lock gate = new();
    private volatile bool isEnabled = true;
    private bool isDecoding;
    private Option<WallpaperDownloadDetails> waiting = Option.None<WallpaperDownloadDetails>();

    /// <summary>Raised with the decoded, display-ready image once a downloaded wallpaper's image is ready.</summary>
    public event EventHandler<WallpaperPreviewImage>? ImageReady;

    /// <summary>Raised with the details of a downloaded wallpaper when <see cref="IsEnabled"/> is <c>false</c>, so the details stay up to date without decoding the image.</summary>
    public event EventHandler<WallpaperInfo>? DetailsReady;

    public ImageDisplayCoordinator(IImageDownloadNotifier notifier, IDownloadedImageDecoder decoder)
    {
        this.decoder = decoder;
        notifier.ImageDownloaded += (_, details) => OnImageDownloaded(details);
    }

    /// <summary>Gets or sets whether downloaded images are decoded for preview. While <c>false</c>, notified images are ignored.</summary>
    public bool IsEnabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    private void OnImageDownloaded(WallpaperDownloadDetails details)
    {
        if (!isEnabled)
        {
            DetailsReady?.Invoke(this, details.Info);

            return;
        }

        lock (gate)
        {
            if (isDecoding)
            {
                waiting = Option.Some(details);

                return;
            }

            isDecoding = true;
        }

        _ = Task.Run(() => DecodeUntilNoneWaiting(details));
    }

    private void DecodeUntilNoneWaiting(WallpaperDownloadDetails details)
    {
        var current = details;
        while (true)
        {
            Publish(current);

            lock (gate)
            {
                if (waiting is not Option<WallpaperDownloadDetails>.Some next)
                {
                    isDecoding = false;

                    return;
                }

                waiting = Option.None<WallpaperDownloadDetails>();
                current = next.Value;
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Best-effort preview on a background thread - any failure just means no preview for this wallpaper and must not stop later previews.")]
    private void Publish(WallpaperDownloadDetails details)
    {
        try
        {
            var pngStream = decoder.DecodeToPng(details.FilePath, PreviewMaximumDimension);
            ImageReady?.Invoke(this, new WallpaperPreviewImage(pngStream, details.Info));
        }
        catch (Exception)
        {
            // Best-effort preview - a decode failure just means no preview for this wallpaper.
        }
    }
}
