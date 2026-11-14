namespace AStarDev.CloudSync.Sync;

/// <summary>Why a strategy cannot be applied to an attention item. The UI disables the strategy and shows the reason (#1406).</summary>
public abstract record ResolutionNotAllowed
{
    private ResolutionNotAllowed()
    {
    }

    /// <summary>The item is a held deletion set, which is confirmed or restored, not resolved with a strategy.</summary>
    public sealed record NotAConflict : ResolutionNotAllowed
    {
        internal NotAConflict()
        {
        }
    }

    /// <summary>The strategy needs the local file, but the conflict recorded none.</summary>
    public sealed record LocalSideAbsent : ResolutionNotAllowed
    {
        internal LocalSideAbsent()
        {
        }
    }

    /// <summary>The strategy needs the cloud file, but the conflict recorded none.</summary>
    public sealed record RemoteSideAbsent : ResolutionNotAllowed
    {
        internal RemoteSideAbsent()
        {
        }
    }
}
