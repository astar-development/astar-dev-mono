namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadPassOptions"/> values.</summary>
public static class UploadPassOptionsFactory
{
    private const int DefaultConcurrentUploads = 8;

    /// <summary>Creates the default options: eight uploads at once.</summary>
    public static UploadPassOptions CreateDefault() => new(DefaultConcurrentUploads);

    /// <summary>Creates options with the given concurrency, at least one.</summary>
    public static UploadPassOptions Create(int maxConcurrentUploads) => new(Math.Max(maxConcurrentUploads, 1));
}
