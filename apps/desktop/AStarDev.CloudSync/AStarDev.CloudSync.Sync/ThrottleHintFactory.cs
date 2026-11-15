namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ThrottleHint"/> cases.</summary>
public static class ThrottleHintFactory
{
    /// <summary>Creates the quiet hint.</summary>
    public static ThrottleHint CreateQuiet() => new ThrottleHint.Quiet();

    /// <summary>Creates the hint that suggests a lower concurrency.</summary>
    public static ThrottleHint CreateSuggestLowerConcurrency() => new ThrottleHint.SuggestLowerConcurrency();
}
