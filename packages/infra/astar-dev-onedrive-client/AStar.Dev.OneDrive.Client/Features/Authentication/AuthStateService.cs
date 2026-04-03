using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     Observable auth state service (AU-05).
///     Publishes account auth state transitions for UI subscribers.
/// </summary>
internal sealed class AuthStateService : IAuthStateService, IDisposable
{
    private readonly Subject<(Guid, AccountAuthState)> _authStateChanged = new();
    private bool _disposed;

    public IObservable<(Guid AccountId, AccountAuthState NewState)> AccountAuthStateChanged => _authStateChanged.AsObservable();

    public void PublishAuthStateChange(Guid accountId, AccountAuthState newState)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AuthStateService));
        _authStateChanged.OnNext((accountId, newState));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _authStateChanged?.Dispose();
        _disposed = true;
    }
}
