using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadTarget"/> cases.</summary>
public static class UploadTargetFactory
{
    /// <summary>Creates the <see cref="UploadTarget.NewFileIn"/> case.</summary>
    public static UploadTarget CreateNewFileIn(Option<RemoteItemId> parent) => new UploadTarget.NewFileIn(parent);

    /// <summary>Creates the <see cref="UploadTarget.ReplacingItem"/> case.</summary>
    public static UploadTarget CreateReplacingItem(RemoteItemId itemId) => new UploadTarget.ReplacingItem(itemId);
}
