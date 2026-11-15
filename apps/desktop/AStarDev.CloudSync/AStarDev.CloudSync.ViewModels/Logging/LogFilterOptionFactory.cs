namespace AStarDev.CloudSync.ViewModels.Logging;

/// <summary>Makes <see cref="LogFilterOption{T}"/>s.</summary>
public static class LogFilterOptionFactory
{
    /// <summary>Makes a choice.</summary>
    /// <typeparam name="T">What the choice stands for.</typeparam>
    /// <param name="value">What is chosen.</param>
    /// <param name="label">The text shown for it.</param>
    public static LogFilterOption<T> Create<T>(T value, string label) => new(value, label);
}
