using AStarDev.CloudSync.DisplaySettings;

namespace AStarDev.CloudSync.TestHelpers.DisplaySettings;

public sealed class InMemoryDisplayPreferencesStore(DisplayPreferences initial) : IDisplayPreferencesStore
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
