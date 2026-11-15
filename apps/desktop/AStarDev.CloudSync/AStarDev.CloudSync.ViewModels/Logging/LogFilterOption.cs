namespace AStarDev.CloudSync.ViewModels.Logging;

/// <summary>One choice in a log viewer drop-down (#1411).</summary>
/// <typeparam name="T">What the choice stands for.</typeparam>
/// <param name="Value">What is chosen.</param>
/// <param name="Label">The text shown for it.</param>
public sealed record LogFilterOption<T>(T Value, string Label);
