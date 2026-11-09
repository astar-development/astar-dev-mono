namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>The colour theme the user has chosen.</summary>
public abstract record AppTheme
{
    private AppTheme()
    {
    }

    /// <summary>The dark theme, the default.</summary>
    public sealed record Dark : AppTheme
    {
        internal Dark()
        {
        }
    }

    /// <summary>The light theme.</summary>
    public sealed record Light : AppTheme
    {
        internal Light()
        {
        }
    }
}
