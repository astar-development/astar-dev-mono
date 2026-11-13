namespace AStarDev.CloudSync.Sync;

/// <summary>The provider capabilities an upload pass uses for one account.</summary>
internal sealed record UploadProviders
{
    internal UploadProviders(IRemoteFileUploader uploader, IRemoteFolderCreator folders, IRemoteItemRemover remover, IRemoteNamePolicy names)
    {
        Uploader = uploader;
        Folders = folders;
        Remover = remover;
        Names = names;
    }

    internal IRemoteFileUploader Uploader { get; }

    internal IRemoteFolderCreator Folders { get; }

    internal IRemoteItemRemover Remover { get; }

    internal IRemoteNamePolicy Names { get; }
}
