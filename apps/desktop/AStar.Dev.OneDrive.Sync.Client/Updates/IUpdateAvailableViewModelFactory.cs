using Velopack;

namespace AStar.Dev.OneDrive.Sync.Client.Updates;

/// <summary>Creates <see cref="UpdateAvailableViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IUpdateAvailableViewModelFactory
{
    /// <summary>Creates a view model for the supplied discovered update.</summary>
    UpdateAvailableViewModel Create(UpdateInfo updateInfo);
}
