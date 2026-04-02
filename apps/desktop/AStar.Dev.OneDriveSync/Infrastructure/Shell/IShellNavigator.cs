namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

/// <summary>
///     Allows feature view-models to programmatically navigate to a <see cref="NavSection"/>
///     without taking a direct dependency on <see cref="MainWindowViewModel"/>.
///     Also observable so that <see cref="MainWindowViewModel"/> can subscribe to navigation requests.
/// </summary>
public interface IShellNavigator : IObservable<NavSection>
{
    /// <summary>Navigates to <paramref name="section"/> if it is available.</summary>
    void Navigate(NavSection section);
}
