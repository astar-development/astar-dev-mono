using AStarDev.CloudSync.Core;

namespace AStarDev.CloudSync.TestsUnit;

internal sealed class RecordingThemeApplier : IThemeApplier
{
    private readonly List<AppTheme> applied = [];

    public IReadOnlyList<AppTheme> Applied => applied;

    public void Apply(AppTheme theme) => applied.Add(theme);
}
