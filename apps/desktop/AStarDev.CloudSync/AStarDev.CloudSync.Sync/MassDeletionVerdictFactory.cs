namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="MassDeletionVerdict"/> cases.</summary>
public static class MassDeletionVerdictFactory
{
    /// <summary>Creates the allowed case.</summary>
    public static MassDeletionVerdict CreateAllowed() => new MassDeletionVerdict.Allowed();

    /// <summary>Creates the held case.</summary>
    public static MassDeletionVerdict CreateHeld() => new MassDeletionVerdict.Held();
}
