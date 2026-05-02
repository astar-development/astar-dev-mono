using System.Collections.Frozen;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public sealed class FeatureAvailabilityService : IFeatureAvailabilityService, IFeatureRegistrar
{
    private readonly HashSet<NavSection> _pendingSections = [];
    private FrozenSet<NavSection>? _frozenSections;

    /// <inheritdoc />
    public bool IsAvailable(NavSection section)
        => _frozenSections?.Contains(section) ?? _pendingSections.Contains(section);

    /// <summary>
    /// Freezes the set of registered sections, preventing any further modifications. This should be called once all available sections have been registered, typically during application startup initialization.
    /// </summary>
    public void Freeze() => _frozenSections = _pendingSections.ToFrozenSet();

    /// <summary>
    /// Registers a navigation section as available. This method can be called multiple times during application initialization to build up the set of available sections. Once the set is frozen, any further calls to this method will throw an exception.
    /// </summary>
    /// <param name="section">The navigation section to register.</param>
    /// <exception cref="InvalidOperationException">Thrown when attempting to register a section after the set has been frozen.</exception>
    public void Register(NavSection section)
    {
        if (_frozenSections is not null) throw new InvalidOperationException("Cannot register sections after the set has been frozen.");

        _pendingSections.Add(section);
    }
}
