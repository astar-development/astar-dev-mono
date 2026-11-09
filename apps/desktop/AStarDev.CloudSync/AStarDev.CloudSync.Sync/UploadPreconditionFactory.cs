namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadPrecondition"/> cases.</summary>
public static class UploadPreconditionFactory
{
    /// <summary>Creates the <see cref="UploadPrecondition.MustNotExist"/> case.</summary>
    public static UploadPrecondition CreateMustNotExist() => new UploadPrecondition.MustNotExist();

    /// <summary>Creates the <see cref="UploadPrecondition.MustMatch"/> case.</summary>
    public static UploadPrecondition CreateMustMatch(string cTag) => new UploadPrecondition.MustMatch(cTag);
}
