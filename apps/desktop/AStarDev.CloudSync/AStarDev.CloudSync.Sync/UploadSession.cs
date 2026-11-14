namespace AStarDev.CloudSync.Sync;

/// <summary>A provider upload session saved so a large upload can continue after a restart (#1611). The upload address is a secret, so it never appears in text.</summary>
public sealed record UploadSession
{
    internal UploadSession(Uri url, DateTimeOffset expiresAt, long sourceLength, DateTime sourceWriteUtc, long confirmedBytes, string target)
    {
        Url = url;
        ExpiresAt = expiresAt;
        SourceLength = sourceLength;
        SourceWriteUtc = sourceWriteUtc;
        ConfirmedBytes = confirmedBytes;
        Target = target;
    }

    /// <summary>The provider's upload address for the session.</summary>
    public Uri Url { get; }

    /// <summary>When the provider drops the session.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>The size of the local file when the session was created.</summary>
    public long SourceLength { get; }

    /// <summary>The local file's last write time (UTC) when the session was created.</summary>
    public DateTime SourceWriteUtc { get; }

    /// <summary>The bytes the provider had confirmed at the last save; the provider's own status is the truth on resume.</summary>
    public long ConfirmedBytes { get; }

    /// <summary>Identifies where the file goes (a new file in a folder, or a replaced item and its content tag), so a session is never reused for another destination.</summary>
    public string Target { get; }

    /// <inheritdoc />
    public override string ToString() => "UploadSession(redacted)";
}
