using System.Reactive;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public interface IFeatureRegistrar
{
    /// <summary>Registers a navigation section as available. Returns an error if called after <see cref="Freeze"/>.</summary>
    Result<Unit, string> Register(NavSection section);

    void Freeze();
}
