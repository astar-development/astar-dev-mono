namespace AStarDev.CloudSync.ViewModels;

/// <summary>Creates <see cref="StatTileTone"/> cases.</summary>
public static class StatTileToneFactory
{
    /// <summary>Creates the normal text colour.</summary>
    public static StatTileTone CreateNormal() => new StatTileTone.Normal();

    /// <summary>Creates the warning colour.</summary>
    public static StatTileTone CreateWarn() => new StatTileTone.Warn();

    /// <summary>Creates the accent colour.</summary>
    public static StatTileTone CreateAccent() => new StatTileTone.Accent();
}
