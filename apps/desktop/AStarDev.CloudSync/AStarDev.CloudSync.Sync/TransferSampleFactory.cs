namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="TransferSample"/> values.</summary>
public static class TransferSampleFactory
{
    /// <summary>Creates a sample. Negative bytes and a negative elapsed time are normalised to zero.</summary>
    public static TransferSample Create(DateTimeOffset at, long bytes, TimeSpan elapsed) => new(at, Math.Max(bytes, 0), elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed);
}
