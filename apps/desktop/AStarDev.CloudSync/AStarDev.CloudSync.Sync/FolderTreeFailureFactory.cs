using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FolderTreeFailure"/> cases.</summary>
public static class FolderTreeFailureFactory
{
    /// <summary>Creates the <see cref="FolderTreeFailure.RemoteFailed"/> case.</summary>
    public static FolderTreeFailure CreateRemoteFailed(ProviderError error) => new FolderTreeFailure.RemoteFailed(error);

    /// <summary>Creates the <see cref="FolderTreeFailure.LocalFailed"/> case.</summary>
    public static FolderTreeFailure CreateLocalFailed(LocalFolderError error) => new FolderTreeFailure.LocalFailed(error);

    /// <summary>Creates the <see cref="FolderTreeFailure.BothFailed"/> case.</summary>
    public static FolderTreeFailure CreateBothFailed(ProviderError remoteError, LocalFolderError localError) => new FolderTreeFailure.BothFailed(remoteError, localError);

    /// <summary>Creates the <see cref="FolderTreeFailure.RulesFailed"/> case.</summary>
    public static FolderTreeFailure CreateRulesFailed(ErrorResponse error) => new FolderTreeFailure.RulesFailed(error);
}
