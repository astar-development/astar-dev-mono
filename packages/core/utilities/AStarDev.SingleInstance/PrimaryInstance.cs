using System.IO.Abstractions;
using System.Net.Sockets;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.SingleInstance;

/// <summary>The running instance: holds the lock and listens for second launches until disposed.</summary>
public sealed class PrimaryInstance : IAsyncDisposable
{
    private const string Location = nameof(PrimaryInstance);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);

    private readonly Stream heldLock;
    private readonly Option<Socket> listener;
    private readonly IFileInfo socketFile;
    private readonly ILogger logger;
    private readonly CancellationTokenSource stopping = new();
    private readonly Task listening;

    private PrimaryInstance(Stream heldLock, Option<Socket> listener, IFileInfo socketFile, ILogger logger)
    {
        this.heldLock = heldLock;
        this.listener = listener;
        this.socketFile = socketFile;
        this.logger = logger;
        listening = listener.Match(socket => Task.Run(() => ListenAsync(socket, stopping.Token), stopping.Token), () => Task.CompletedTask);
    }

    /// <summary>Starts listening on <paramref name="listener"/>; the instance owns the lock, the listener and the socket file from now on.</summary>
    internal static PrimaryInstance Start(Stream heldLock, Option<Socket> listener, IFileInfo socketFile, ILogger logger) => new(heldLock, listener, socketFile, logger);

    /// <summary>Raised on a thread-pool thread when a second launch asks this instance to show its window.</summary>
    public event EventHandler? ActivationRequested;

    /// <summary>Stops listening, removes the socket file and releases the lock.</summary>
    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync().ConfigureAwait(false);
        listener.Match(socket => Dispose(socket), () => Unit.Instance);
        await listening.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        _ = Try.Run(() => DeleteSocketFile(socketFile));
        await heldLock.DisposeAsync().ConfigureAwait(false);
        stopping.Dispose();
    }

    private static Unit Dispose(Socket socket)
    {
        socket.Dispose();

        return Unit.Instance;
    }

    private static Unit DeleteSocketFile(IFileInfo file)
    {
        file.Delete();

        return Unit.Instance;
    }

    private async Task ListenAsync(Socket socket, CancellationToken cancellationToken)
    {
        var keepListening = true;
        while (keepListening && !cancellationToken.IsCancellationRequested)
        {
            keepListening = await Attempt.RunAsync(() => socket.AcceptAsync(cancellationToken).AsTask(), cancellationToken)
                .MatchAsync(client => AnswerAsync(client, cancellationToken), () => ListenerStopped(cancellationToken))
                .ConfigureAwait(false);
        }
    }

    private bool ListenerStopped(CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
            LogMessage.Warning(logger, Location, "The hand-off listener stopped.");

        return false;
    }

    private async Task<bool> AnswerAsync(Socket client, CancellationToken cancellationToken)
    {
        using (client)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RequestTimeout);
            var request = await Attempt.RunAsync(() => HandOffProtocol.ReceiveAsync(client, timeout.Token), timeout.Token).ConfigureAwait(false);

            if (request.Filter(message => message == HandOffProtocol.ShowRequest).Match(_ => true, () => false))
            {
                _ = Try.Run(RaiseActivationRequested);
                _ = await Attempt.RunAsync(() => SendAcknowledgementAsync(client, timeout.Token), timeout.Token).ConfigureAwait(false);
            }
        }

        return true;
    }

    private Unit RaiseActivationRequested()
    {
        ActivationRequested?.Invoke(this, EventArgs.Empty);

        return Unit.Instance;
    }

    private static async Task<Unit> SendAcknowledgementAsync(Socket client, CancellationToken cancellationToken)
    {
        await HandOffProtocol.SendAsync(client, HandOffProtocol.Acknowledgement, cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }
}
