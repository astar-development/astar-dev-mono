using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Finds the provider capabilities for an account's provider.</summary>
/// <param name="uploaders">Finds the file uploader.</param>
/// <param name="folders">Finds the folder creator.</param>
/// <param name="removers">Finds the item remover.</param>
/// <param name="names">Finds the name policy.</param>
internal sealed class UploadPassProviders(IProviderCapability<IRemoteFileUploader> uploaders, IProviderCapability<IRemoteFolderCreator> folders, IProviderCapability<IRemoteItemRemover> removers, IProviderCapability<IRemoteNamePolicy> names)
{
    internal Task<Result<UploadProviders, UploadPassFailure>> ResolveAsync(AccountId accountId, CancellationToken cancellationToken) =>
        uploaders.ForAccountAsync(accountId, cancellationToken)
            .BindAsync(uploader => folders.ForAccountAsync(accountId, cancellationToken)
                .BindAsync(creator => removers.ForAccountAsync(accountId, cancellationToken)
                    .BindAsync(remover => names.ForAccountAsync(accountId, cancellationToken)
                        .BindAsync(policy => Task.FromResult(Result.Success<UploadProviders, ProviderError>(UploadProvidersFactory.Create(uploader, creator, remover, policy)))))))
            .MatchAsync(Result.Success<UploadProviders, UploadPassFailure>, error => Result.Failure<UploadProviders, UploadPassFailure>(FailureOf(error)));

    private static UploadPassFailure FailureOf(ProviderError error) => error is ProviderError.AuthenticationRequired ? UploadPassFailureFactory.CreateAuthenticationRequired() : UploadPassFailureFactory.CreateUnexpected();
}
