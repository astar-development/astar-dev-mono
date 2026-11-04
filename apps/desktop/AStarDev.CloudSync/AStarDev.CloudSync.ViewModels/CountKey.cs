namespace AStarDev.CloudSync.ViewModels;

/// <summary>Picks the singular or plural form of a localisation key from a count.</summary>
internal static class CountKey
{
    /// <summary>Returns <paramref name="baseKey"/> with ".One" when <paramref name="count"/> is 1, otherwise with ".Many".</summary>
    /// <param name="baseKey">The key without its ".One" or ".Many" suffix, e.g. "Stats.Conflicts".</param>
    /// <param name="count">The number the label describes.</param>
    public static string For(string baseKey, int count) => count == 1 ? $"{baseKey}.One" : $"{baseKey}.Many";
}
