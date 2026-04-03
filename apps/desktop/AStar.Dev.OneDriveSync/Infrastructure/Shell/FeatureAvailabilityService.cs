using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

public sealed class FeatureAvailabilityService : IFeatureAvailabilityService
{
    private readonly HashSet<NavSection> _pendingSections = [];
    private FrozenSet<NavSection>? _frozenSections;

    public bool IsAvailable(NavSection section)
        => _frozenSections?.Contains(section) ?? _pendingSections.Contains(section);

    public void Freeze() => _frozenSections = _pendingSections.ToFrozenSet();

    public void Register(NavSection section)
    {
        if(_frozenSections is not null) throw new InvalidOperationException("Cannot register sections after the set has been frozen.");

        _pendingSections.Add(section);
    }
}
