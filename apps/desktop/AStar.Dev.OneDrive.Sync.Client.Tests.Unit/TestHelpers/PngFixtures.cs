namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;

/// <summary>Pre-built minimal PNG byte payloads for unit tests that need real image data.</summary>
internal static class PngFixtures
{
    /// <summary>1×1 pixel red PNG — valid for <c>Bitmap.DecodeToWidth</c> without hitting the filesystem.</summary>
    public static byte[] OneByOnePng { get; } = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
}
