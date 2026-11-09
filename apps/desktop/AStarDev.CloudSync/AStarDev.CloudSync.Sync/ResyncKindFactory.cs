namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ResyncKind"/> cases.</summary>
public static class ResyncKindFactory
{
    /// <summary>Creates the <see cref="ResyncKind.ApplyDifferences"/> case.</summary>
    public static ResyncKind CreateApplyDifferences() => new ResyncKind.ApplyDifferences();

    /// <summary>Creates the <see cref="ResyncKind.UploadDifferences"/> case.</summary>
    public static ResyncKind CreateUploadDifferences() => new ResyncKind.UploadDifferences();

    /// <summary>Creates the <see cref="ResyncKind.Unknown"/> case.</summary>
    public static ResyncKind CreateUnknown() => new ResyncKind.Unknown();
}
