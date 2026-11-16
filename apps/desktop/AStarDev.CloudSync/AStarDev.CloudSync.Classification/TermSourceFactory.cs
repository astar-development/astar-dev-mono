namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="TermSource"/> cases.</summary>
public static class TermSourceFactory
{
    /// <summary>Creates the Seed case.</summary>
    public static TermSource CreateSeed() => new TermSource.Seed();
    /// <summary>Creates the User case.</summary>
    public static TermSource CreateUser() => new TermSource.User();
    /// <summary>Creates the Auto case.</summary>
    public static TermSource CreateAuto() => new TermSource.Auto();
    /// <summary>Creates the Suggested case.</summary>
    public static TermSource CreateSuggested() => new TermSource.Suggested();
}
