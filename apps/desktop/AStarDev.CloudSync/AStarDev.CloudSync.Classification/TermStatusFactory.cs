namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="TermStatus"/> cases.</summary>
public static class TermStatusFactory
{
    /// <summary>Creates the Active case.</summary>
    public static TermStatus CreateActive() => new TermStatus.Active();
    /// <summary>Creates the Ignored case.</summary>
    public static TermStatus CreateIgnored() => new TermStatus.Ignored();
}
