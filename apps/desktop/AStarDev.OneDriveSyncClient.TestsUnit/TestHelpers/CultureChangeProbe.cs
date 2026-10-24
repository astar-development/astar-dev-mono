using System.ComponentModel;
using System.Globalization;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;

internal static class CultureChangeProbe
{
    public static IReadOnlyList<string?> RaisedPropertyNamesAfterCultureChange(INotifyPropertyChanged viewModel, ILocalizationService localization)
    {
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        localization.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(localization, new CultureInfo("en-GB"));

        return raised;
    }

    public static bool Refreshes(IReadOnlyList<string?> raised, string propertyName) => raised.Contains(propertyName) || raised.Contains(string.Empty) || raised.Contains(null);
}
