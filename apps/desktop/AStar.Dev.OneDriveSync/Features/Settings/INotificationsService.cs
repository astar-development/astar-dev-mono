using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDriveSync.Features.Settings;

/// <summary>Manages the OS-notification toggle: persistence and in-memory state (ST-03).</summary>
public interface INotificationsService
{
    /// <summary>Whether OS notifications are currently enabled.</summary>
    bool NotificationsEnabled { get; }

    /// <summary>Loads the persisted notification preference. Falls back to <see langword="true"/> on failure.</summary>
    Task InitialiseAsync(CancellationToken ct = default);

    /// <summary>Persists <paramref name="enabled"/> and updates the in-memory state immediately.</summary>
    Task<Result<bool, ErrorResponse>> SetEnabledAsync(bool enabled, CancellationToken ct = default);
}
