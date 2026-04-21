namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

/// <summary>
/// Thrown when the stored delta link returns HTTP 410 Gone, indicating the link has expired
/// and a full re-enumeration is required to obtain a fresh link.
/// </summary>
public sealed class DeltaLinkExpiredException : Exception
{
    public DeltaLinkExpiredException() : base("The delta link has expired. A full re-enumeration is required.") { }
}
