namespace AStarDev.CloudSync.ViewModels;

/// <summary>The version of the running app, as shown to the user, e.g. "1.71.1".</summary>
public sealed record AppVersion
{
    internal AppVersion(string text) => Text = text;

    /// <summary>The version without a leading "v" and without build metadata.</summary>
    public string Text { get; }
}
