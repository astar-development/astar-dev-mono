namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ResolutionNotAllowed"/> cases.</summary>
public static class ResolutionNotAllowedFactory
{
    /// <summary>Creates the <see cref="ResolutionNotAllowed.NotAConflict"/> case.</summary>
    public static ResolutionNotAllowed CreateNotAConflict() => new ResolutionNotAllowed.NotAConflict();

    /// <summary>Creates the <see cref="ResolutionNotAllowed.LocalSideAbsent"/> case.</summary>
    public static ResolutionNotAllowed CreateLocalSideAbsent() => new ResolutionNotAllowed.LocalSideAbsent();

    /// <summary>Creates the <see cref="ResolutionNotAllowed.RemoteSideAbsent"/> case.</summary>
    public static ResolutionNotAllowed CreateRemoteSideAbsent() => new ResolutionNotAllowed.RemoteSideAbsent();
}
