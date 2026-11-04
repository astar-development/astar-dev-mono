namespace AStarDev.CloudSync.ViewModels.Overview;

/// <summary>The colour of a summary figure.</summary>
public abstract record StatTileTone
{
    private StatTileTone()
    {
    }

    /// <summary>The normal text colour.</summary>
    public sealed record Normal : StatTileTone;

    /// <summary>The warning colour, e.g. for conflicts waiting for the user.</summary>
    public sealed record Warn : StatTileTone;

    /// <summary>The accent colour, e.g. for the overall status.</summary>
    public sealed record Accent : StatTileTone;
}
