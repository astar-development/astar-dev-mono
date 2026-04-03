using System;
using System.Reactive.Subjects;

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

/// <summary>
///     Forwards navigation requests to subscribers (typically <c>MainWindowViewModel</c>).
/// </summary>
internal sealed class ShellNavigator : IShellNavigator, IDisposable
{
    private readonly Subject<NavSection> _requests = new();

    public void Navigate(NavSection section) => _requests.OnNext(section);

    public IDisposable Subscribe(IObserver<NavSection> observer) => _requests.Subscribe(observer);

    public void Dispose() => _requests.Dispose();
}
