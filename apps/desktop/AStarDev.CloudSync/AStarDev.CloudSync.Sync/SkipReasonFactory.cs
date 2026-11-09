namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SkipReason"/> cases.</summary>
public static class SkipReasonFactory
{
    /// <summary>Creates the <see cref="SkipReason.Hidden"/> case.</summary>
    public static SkipReason CreateHidden() => new SkipReason.Hidden();

    /// <summary>Creates the <see cref="SkipReason.DotPrefixed"/> case.</summary>
    public static SkipReason CreateDotPrefixed() => new SkipReason.DotPrefixed();

    /// <summary>Creates the <see cref="SkipReason.SymbolicLink"/> case.</summary>
    public static SkipReason CreateSymbolicLink() => new SkipReason.SymbolicLink();

    /// <summary>Creates the <see cref="SkipReason.AppTemporary"/> case.</summary>
    public static SkipReason CreateAppTemporary() => new SkipReason.AppTemporary();
}
