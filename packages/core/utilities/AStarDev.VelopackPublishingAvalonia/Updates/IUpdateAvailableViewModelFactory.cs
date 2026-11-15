using Velopack;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Creates <see cref="UpdateAvailableViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IUpdateAvailableViewModelFactory
{
    /// <summary>Creates a view model for the supplied discovered update.</summary>
    UpdateAvailableViewModel Create(UpdateInfo updateInfo);

    /// <summary>Creates a view model for an update that has been deferred for too long: the dialog warns that it is about to be installed and cannot be deferred.</summary>
    UpdateAvailableViewModel CreateForced(UpdateInfo updateInfo);
}
