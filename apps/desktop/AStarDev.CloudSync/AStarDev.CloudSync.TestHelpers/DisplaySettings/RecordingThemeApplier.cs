using AStarDev.CloudSync.Core.DisplaySettings;

namespace AStarDev.CloudSync.TestHelpers.DisplaySettings;

public sealed class RecordingThemeApplier : IThemeApplier
{
    private readonly List<AppTheme> applied = [];

    public IReadOnlyList<AppTheme> Applied => applied;

    public void Apply(AppTheme theme) => applied.Add(theme);
}
