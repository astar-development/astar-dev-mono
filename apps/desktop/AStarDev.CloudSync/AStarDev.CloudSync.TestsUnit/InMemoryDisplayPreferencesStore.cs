using AStarDev.CloudSync.Core;

namespace AStarDev.CloudSync.TestsUnit;

internal sealed class InMemoryDisplayPreferencesStore(DisplayPreferences initial) : IDisplayPreferencesStore
{
    public InMemoryDisplayPreferencesStore() : this(DisplayPreferencesFactory.CreateDefault())
    {
    }

    public DisplayPreferences Saved { get; private set; } = initial;

    public int SaveCount { get; private set; }

    public DisplayPreferences Load() => Saved;

    public void Save(DisplayPreferences preferences)
    {
        Saved = preferences;
        SaveCount++;
    }
}
