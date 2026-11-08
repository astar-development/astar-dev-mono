using System.Net.Sockets;
using System.Text;

namespace AStarDev.SingleInstance;

/// <summary>The one-line messages a second launch and the running instance exchange.</summary>
internal static class HandOffProtocol
{
    private const int MaxMessageBytes = 16;

    /// <summary>The request a second launch sends.</summary>
    public const string ShowRequest = "show";

    /// <summary>The answer the running instance sends.</summary>
    public const string Acknowledgement = "ok";

    /// <summary>Creates an unbound Unix domain stream socket.</summary>
    public static Socket CreateSocket() => new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

    /// <summary>Creates a socket listening on the path; the socket is disposed again if it cannot bind.</summary>
    /// <param name="socketPath">The socket file path.</param>
    public static Socket CreateListener(string socketPath)
    {
        using var candidate = new SocketOwner(CreateSocket());
        candidate.Socket.Bind(new UnixDomainSocketEndPoint(socketPath));
        candidate.Socket.Listen();

        return candidate.Release();
    }

    /// <summary>Sends one message.</summary>
    /// <param name="socket">The connected socket.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">Cancels the send.</param>
    public static async Task SendAsync(Socket socket, string message, CancellationToken cancellationToken) => _ = await socket.SendAsync(Encoding.ASCII.GetBytes(message + "\n"), SocketFlags.None, cancellationToken).ConfigureAwait(false);

    /// <summary>Receives one message.</summary>
    /// <param name="socket">The connected socket.</param>
    /// <param name="cancellationToken">Cancels the receive.</param>
    public static async Task<string> ReceiveAsync(Socket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[MaxMessageBytes];
        var received = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);

        return Encoding.ASCII.GetString(buffer, 0, received).Trim();
    }
}
