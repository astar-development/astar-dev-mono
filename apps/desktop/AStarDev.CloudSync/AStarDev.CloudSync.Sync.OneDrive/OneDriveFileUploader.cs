using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using static AStarDev.CloudSync.Sync.OneDrive.OneDriveUploadSupport;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="keeper">Resumes, saves and deletes the session of a large upload.</param>
/// <param name="creator">Creates the upload session.</param>
/// <param name="fragments">Sends a large file fragment by fragment.</param>
/// <param name="empty">Uploads a zero-byte file.</param>
/// <param name="logger">Reports counts and status codes only, never names or URLs.</param>
internal sealed class OneDriveFileUploader(OneDriveTransferHttp http, OneDriveUploadSessionKeeper keeper, OneDriveUploadSessionCreator creator, OneDriveFragmentUploader fragments, OneDriveEmptyFileUploader empty, ILogger<OneDriveFileUploader> logger) : IRemoteFileUploader
{
    /// <inheritdoc />
    public async Task<Result<UploadedFile, ProviderError>> UploadAsync(AccountId accountId, UploadTarget target, IFileInfo source, UploadPrecondition precondition, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);
        var transfer = new UploadTransfer(accountId, target, source, precondition, progress);
        var running = http.CredentialsAsync(accountId, outcome => OneDriveTransferLog.TokenUnavailable(logger, outcome), cancellationToken)
            .BindAsync(credentials => RunAsync(transfer, credentials, cancellationToken));
        await ((Task)running).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (running.IsCanceled)
        {
            await CancelSessionAsync(transfer).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return await running.ConfigureAwait(false);
    }

    private Task<Result<UploadedFile, ProviderError>> RunAsync(UploadTransfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        transfer.Source.Refresh();
        transfer.Length = transfer.Source.Length;
        transfer.LastWriteUtc = transfer.Source.LastWriteTimeUtc;

        return transfer.Length == 0 ? empty.UploadEmptyAsync(transfer, credentials, cancellationToken) : UploadInFragmentsAsync(transfer, credentials, cancellationToken);
    }

    private async Task<Result<UploadedFile, ProviderError>> UploadInFragmentsAsync(UploadTransfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        var created = await EnsureSessionAsync(transfer, credentials, cancellationToken).ConfigureAwait(false);

        return await created.Match(_ => fragments.SendFragmentsAsync(transfer, credentials, cancellationToken), error => Task.FromResult(Result.Failure<UploadedFile, ProviderError>(error))).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ProviderError>> EnsureSessionAsync(UploadTransfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        var resumed = await keeper.ResumeSavedAsync(transfer, cancellationToken).ConfigureAwait(false);

        return await resumed.Match(
            _ => transfer.Session.Match(_ => Task.FromResult(resumed), () => creator.CreateSessionAsync(transfer, credentials, cancellationToken)),
            _ => Task.FromResult(resumed)).ConfigureAwait(false);
    }

    private async Task CancelSessionAsync(UploadTransfer transfer)
    {
        OneDriveTransferLog.UploadCancelled(logger);
        if (!IsLarge(transfer))
            await keeper.DeleteSessionAsync(transfer, CancellationToken.None).ConfigureAwait(false);
    }
}
