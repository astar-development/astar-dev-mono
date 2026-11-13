namespace AStarDev.CloudSync.Sync;

internal static class UploadProvidersFactory
{
    internal static UploadProviders Create(IRemoteFileUploader uploader, IRemoteFolderCreator folders, IRemoteItemRemover remover, IRemoteNamePolicy names) => new(uploader, folders, remover, names);
}
