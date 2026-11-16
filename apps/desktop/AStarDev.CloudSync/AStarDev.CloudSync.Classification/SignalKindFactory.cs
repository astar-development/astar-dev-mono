namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="SignalKind"/> cases.</summary>
public static class SignalKindFactory
{
    /// <summary>Creates the Extension case.</summary>
    public static SignalKind CreateExtension() => new SignalKind.Extension();
    /// <summary>Creates the FolderName case.</summary>
    public static SignalKind CreateFolderName() => new SignalKind.FolderName();
    /// <summary>Creates the Vocabulary case.</summary>
    public static SignalKind CreateVocabulary() => new SignalKind.Vocabulary();
}
