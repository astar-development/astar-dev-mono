using System.Net.Sockets;

namespace AStarDev.SingleInstance;

/// <summary>Disposes a socket unless ownership is released, so a failed bind never leaks it.</summary>
/// <param name="socket">The socket being set up.</param>
internal sealed class SocketOwner(Socket socket) : IDisposable
{
    private bool released;

    /// <summary>The socket being set up.</summary>
    public Socket Socket { get; } = socket;

    /// <summary>Hands the socket to the caller; it is no longer disposed here.</summary>
    public Socket Release()
    {
        released = true;

        return Socket;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!released)
            Socket.Dispose();
    }
}
