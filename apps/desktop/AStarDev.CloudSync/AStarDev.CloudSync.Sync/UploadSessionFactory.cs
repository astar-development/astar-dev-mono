namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadSession"/> values.</summary>
public static class UploadSessionFactory
{
    /// <summary>Creates a session. The confirmed bytes are kept between zero and the source length, and the write time is stored as UTC.</summary>
    /// <param name="url">The provider's upload address.</param>
    /// <param name="expiresAt">When the provider drops the session.</param>
    /// <param name="sourceLength">The local file's size.</param>
    /// <param name="sourceWriteUtc">The local file's last write time.</param>
    /// <param name="confirmedBytes">The bytes the provider confirmed.</param>
    /// <param name="target">Identifies the destination.</param>
    public static UploadSession Create(Uri url, DateTimeOffset expiresAt, long sourceLength, DateTime sourceWriteUtc, long confirmedBytes, string target) =>
        new(url, expiresAt, Math.Max(sourceLength, 0), DateTime.SpecifyKind(sourceWriteUtc, DateTimeKind.Utc), Math.Clamp(confirmedBytes, 0, Math.Max(sourceLength, 0)), target);
}
