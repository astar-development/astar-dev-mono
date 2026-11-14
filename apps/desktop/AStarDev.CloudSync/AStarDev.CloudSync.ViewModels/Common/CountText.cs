using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Common;

/// <summary>Formats a count that may be unknown.</summary>
internal static class CountText
{
    /// <summary>Returns the count formatted for the culture, or the localised unknown marker (an em dash) when it is none.</summary>
    /// <param name="count">The count, or none when it is unknown.</param>
    /// <param name="localization">Supplies the culture and the unknown marker.</param>
    public static string Format(Option<int> count, ILocalizationService localization) => count.Match(known => known.ToString("N0", localization.CurrentCulture), () => localization.GetLocal("Count.Unknown"));
}
