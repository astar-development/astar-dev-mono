using System.IO.Abstractions;
using System.Net.Sockets;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.SingleInstance;

/// <summary>Claims the lock file and hands off to the running instance over a Unix domain socket.</summary>
/// <param name="logger">Reports hand-off results and lock failures, without paths.</param>
internal sealed class SingleInstanceGuard(ILogger<SingleInstanceGuard> logger) : ISingleInstanceGuard
{
    private const string Location = nameof(SingleInstanceGuard);
    private const string UnexpectedReply = "UnexpectedReply";

    private static readonly HashSet<int> SharingViolationCodes =
    [
        unchecked((int)0x80070020),
        unchecked((int)0x80070021),
        11,
        35,
    ];

    /// <inheritdoc />
    public Task<InstanceClaim> ClaimAsync(SingleInstanceOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fileSystem = options.DataFolder.FileSystem;
        var lockPath = fileSystem.Path.Join(options.DataFolder.FullName, options.Name + ".lock");
        var socketFile = fileSystem.FileInfo.New(fileSystem.Path.Join(options.DataFolder.FullName, options.Name + ".sock"));

        return Try.Run(() => CreateFolder(options.DataFolder))
            .Match(
                _ => Try.Run(() => (Stream)fileSystem.FileStream.New(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    .Match(heldLock => Task.FromResult(BecomePrimary(heldLock, socketFile)), exception => HandOffOrFailAsync(exception, socketFile, options.HandOffTimeout, cancellationToken)),
                exception => Task.FromResult(LockUnavailable(exception)));
    }

    private static Unit CreateFolder(IDirectoryInfo folder)
    {
        folder.Create();

        return Unit.Instance;
    }

    private InstanceClaim BecomePrimary(Stream heldLock, IFileInfo socketFile)
    {
        _ = Try.Run(() => DeleteStaleSocketFile(socketFile));
        var listener = Listen(socketFile.FullName);
        _ = listener.Match(_ => Unit.Instance, HandOffUnavailable);

        return InstanceClaimFactory.CreatePrimary(PrimaryInstance.Start(heldLock, listener, socketFile, logger));
    }

    private static Unit DeleteStaleSocketFile(IFileInfo socketFile)
    {
        socketFile.Delete();

        return Unit.Instance;
    }

    private static Option<Socket> Listen(string socketPath) => Try.Run(() => HandOffProtocol.CreateListener(socketPath)).Match(Option.Some, _ => Option.None<Socket>());

    private Unit HandOffUnavailable()
    {
        LogMessage.Warning(logger, Location, "The hand-off socket could not be opened; second launches will not be answered.");

        return Unit.Instance;
    }

    private Task<InstanceClaim> HandOffOrFailAsync(Exception exception, IFileInfo socketFile, TimeSpan timeout, CancellationToken cancellationToken) =>
        IsHeldByAnotherInstance(exception)
            ? HandOffAsync(socketFile.FullName, timeout, cancellationToken)
            : Task.FromResult(LockUnavailable(exception));

    private static bool IsHeldByAnotherInstance(Exception exception) => exception.GetType() == typeof(IOException) && SharingViolationCodes.Contains(exception.HResult);

    private async Task<InstanceClaim> HandOffAsync(string socketPath, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        var answer = await Attempt.RunAsync(() => AskToShowAsync(socketPath, deadline.Token), deadline.Token).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return answer.Match(reply => reply == HandOffProtocol.Acknowledgement ? HandedOff() : NotAnswered(UnexpectedReply), exception => NotAnswered(exception.ErrorType()));
    }

    private static async Task<string> AskToShowAsync(string socketPath, CancellationToken cancellationToken)
    {
        using var socket = HandOffProtocol.CreateSocket();
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken).ConfigureAwait(false);
        await HandOffProtocol.SendAsync(socket, HandOffProtocol.ShowRequest, cancellationToken).ConfigureAwait(false);

        return await HandOffProtocol.ReceiveAsync(socket, cancellationToken).ConfigureAwait(false);
    }

    private InstanceClaim HandedOff()
    {
        LogMessage.Information(logger, Location, "Another instance is running and was asked to show its window.");

        return InstanceClaimFactory.CreateHandedOff();
    }

    private InstanceClaim NotAnswered(string errorType)
    {
        LogMessage.Warning(logger, Location, $"Another instance holds the lock but did not answer the hand-off ({errorType}).");

        return InstanceClaimFactory.CreateNotAnswered();
    }

    private InstanceClaim LockUnavailable(Exception exception)
    {
        LogMessage.Error(logger, "The single-instance lock could not be created.", exception.ErrorType());

        return InstanceClaimFactory.CreateLockUnavailable();
    }
}
