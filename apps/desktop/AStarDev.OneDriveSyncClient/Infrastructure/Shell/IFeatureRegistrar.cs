using System.Reactive;
using Unit = System.Reactive.Unit;
using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

public interface IFeatureRegistrar
{
    /// <summary>Registers a navigation section as available. Returns an error if called after <see cref="Freeze"/>.</summary>
    Result<Unit, string> Register(NavSection section);

    void Freeze();
}
