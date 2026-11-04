using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

internal static class Snapshot
{
    public const string LinuxOnlyReason = "Snapshot baselines are rendered on Linux; font rasterisation differs on other operating systems.";

    private const int ChannelTolerance = 8;
    private const int MaxDifferingPixels = 16;
    private const int BytesPerPixel = 4;
    private static readonly string[] baselineFolderSegments = ["apps", "desktop", "AStarDev.CloudSync", "AStarDev.CloudSync.TestsUnit", "Snapshots", "Baselines"];
    private static readonly Vector dpi = new(96, 96);

    public static bool IsUpdatingBaselines => Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1";

    public static void ShouldMatchBaseline(Window window, ThemeVariant theme, string name, int width = 1280, int height = 800)
    {
        if (IsUpdatingBaselines)
        {
            WriteBaseline(window, theme, name, width, height);

            return;
        }

        switch (Compare(window, theme, name, width, height))
        {
            case SnapshotResult.Mismatch mismatch:
                throw new SnapshotMismatchException($"Snapshot '{name}' differs from its baseline in {mismatch.DifferingPixels} pixels. Actual: {mismatch.Actual.FullName} Diff: {mismatch.Diff.FullName} Baseline: {BaselineFile(name).FullName}. If the change is intended, re-run with UPDATE_SNAPSHOTS=1.");
            case SnapshotResult.MissingBaseline missing:
                throw new SnapshotMismatchException($"Snapshot '{name}' has no baseline at {BaselineFile(name).FullName}. Actual: {missing.Actual.FullName}. Re-run with UPDATE_SNAPSHOTS=1 to create it.");
        }
    }

    public static SnapshotResult Compare(Window window, ThemeVariant theme, string name, int width = 1280, int height = 800)
    {
        using var actual = Render(window, theme, width, height);
        var outputFolder = OutputFolder();
        var actualFile = new FileInfo(Path.Join(outputFolder.FullName, $"{name}.actual.png"));
        actual.Save(actualFile.FullName, PngBitmapEncoderOptions.Default);
        actualFile.Refresh();

        var baselineFile = BaselineFile(name);
        if (!baselineFile.Exists)
            return SnapshotResultFactory.CreateMissingBaseline(actualFile);

        using var baseline = new Bitmap(baselineFile.FullName);
        var actualPixels = ReadPixels(actual);
        var baselinePixels = ReadPixels(baseline);
        int differingPixels = CountDifferingPixels(actualPixels, baselinePixels, actual.PixelSize, baseline.PixelSize);
        if (differingPixels <= MaxDifferingPixels)
            return SnapshotResultFactory.CreateMatch();

        var diffFile = new FileInfo(Path.Join(outputFolder.FullName, $"{name}.diff.png"));
        WriteDiff(actualPixels, baselinePixels, actual.PixelSize, baseline.PixelSize, diffFile);

        return SnapshotResultFactory.CreateMismatch(actualFile, diffFile, differingPixels);
    }

    private static void WriteBaseline(Window window, ThemeVariant theme, string name, int width, int height)
    {
        using var frame = Render(window, theme, width, height);
        var baselineFile = BaselineFile(name);
        baselineFile.Directory!.Create();
        frame.Save(baselineFile.FullName, PngBitmapEncoderOptions.Default);
    }

    private static WriteableBitmap Render(Window window, ThemeVariant theme, int width, int height)
    {
        CultureInfo.CurrentCulture = new CultureInfo("en-GB");
        CultureInfo.CurrentUICulture = new CultureInfo("en-GB");
        window.Width = width;
        window.Height = height;
        window.RequestedThemeVariant = theme;
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The headless platform returned no frame. TestApp must use Skia with UseHeadlessDrawing = false.");
        window.Close();

        return frame;
    }

    private static PixelBuffer ReadPixels(Bitmap bitmap)
    {
        using var converted = new WriteableBitmap(bitmap.PixelSize, dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var framebuffer = converted.Lock();
        bitmap.CopyPixels(framebuffer);
        var bytes = new byte[framebuffer.RowBytes * bitmap.PixelSize.Height];
        Marshal.Copy(framebuffer.Address, bytes, 0, bytes.Length);

        return new PixelBuffer(bytes, framebuffer.RowBytes);
    }

    private static int CountDifferingPixels(PixelBuffer actual, PixelBuffer baseline, PixelSize actualSize, PixelSize baselineSize)
    {
        if (actualSize != baselineSize)
            return actualSize.Width * actualSize.Height;

        int differing = 0;
        for (int row = 0; row < actualSize.Height; row++)
        {
            for (int column = 0; column < actualSize.Width; column++)
            {
                if (PixelDiffers(actual, baseline, row, column))
                    differing++;
            }
        }

        return differing;
    }

    private static bool PixelDiffers(PixelBuffer actual, PixelBuffer baseline, int row, int column)
    {
        int actualOffset = (row * actual.RowBytes) + (column * BytesPerPixel);
        int baselineOffset = (row * baseline.RowBytes) + (column * BytesPerPixel);
        for (int channel = 0; channel < BytesPerPixel; channel++)
        {
            if (Math.Abs(actual.Bytes[actualOffset + channel] - baseline.Bytes[baselineOffset + channel]) > ChannelTolerance)
                return true;
        }

        return false;
    }

    private static void WriteDiff(PixelBuffer actual, PixelBuffer baseline, PixelSize actualSize, PixelSize baselineSize, FileInfo diffFile)
    {
        using var diff = new WriteableBitmap(actualSize, dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var framebuffer = diff.Lock())
        {
            var bytes = new byte[framebuffer.RowBytes * actualSize.Height];
            bool sameSize = actualSize == baselineSize;
            for (int row = 0; row < actualSize.Height; row++)
            {
                for (int column = 0; column < actualSize.Width; column++)
                {
                    int offset = (row * framebuffer.RowBytes) + (column * BytesPerPixel);
                    int actualOffset = (row * actual.RowBytes) + (column * BytesPerPixel);
                    bool differs = !sameSize || PixelDiffers(actual, baseline, row, column);
                    bytes[offset] = differs ? (byte)0 : (byte)(actual.Bytes[actualOffset] / 3);
                    bytes[offset + 1] = differs ? (byte)0 : (byte)(actual.Bytes[actualOffset + 1] / 3);
                    bytes[offset + 2] = differs ? (byte)255 : (byte)(actual.Bytes[actualOffset + 2] / 3);
                    bytes[offset + 3] = 255;
                }
            }

            Marshal.Copy(bytes, 0, framebuffer.Address, bytes.Length);
        }

        diff.Save(diffFile.FullName, PngBitmapEncoderOptions.Default);
        diffFile.Refresh();
    }

    private static FileInfo BaselineFile(string name) => new(Path.Join([RepositoryRoot().FullName, .. baselineFolderSegments, $"{name}.png"]));

    private static DirectoryInfo OutputFolder()
    {
        var folder = new DirectoryInfo(Path.Join(RepositoryRoot().FullName, "artifacts", "snapshots"));
        folder.Create();

        return folder;
    }

    private static DirectoryInfo RepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Join(folder.FullName, "AStarDev.slnx")))
            folder = folder.Parent;

        return folder ?? throw new InvalidOperationException($"Could not find AStarDev.slnx above {AppContext.BaseDirectory}.");
    }

    private sealed record PixelBuffer(byte[] Bytes, int RowBytes);
}
