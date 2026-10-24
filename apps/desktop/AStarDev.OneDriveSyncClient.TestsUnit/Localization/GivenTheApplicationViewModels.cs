using System.Reflection;
using AStarDev.OneDriveSyncClient.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Localization;

public sealed class GivenTheApplicationViewModels
{
    private static readonly IReadOnlySet<string> selfRefreshingViewModels = new HashSet<string>(StringComparer.Ordinal)
    {
        "StatusBarViewModel",
        "ConflictItemViewModel",
        "FolderTreeNodeViewModel",
        "AccountSyncSettingsViewModel",
        "SettingsViewModel",
        "AddAccountWizardViewModel",
        "ClassificationText"
    };

    [Fact]
    public void when_a_view_model_displays_localised_text_then_it_refreshes_itself_when_the_culture_changes()
    {
        var offenders = typeof(ILocalizationService).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true } && type.IsAssignableTo(typeof(ObservableObject)))
            .Where(type => type.GetConstructors().Any(constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(ILocalizationService) || parameter.ParameterType.Name == "IAccountFilesViewServices")))
            .Where(type => !selfRefreshingViewModels.Contains(type.Name))
            .Where(type => !InheritsFrom(type, "LocalizedObservableObject"))
            .Select(type => type.Name)
            .Order()
            .ToList();

        offenders.ShouldBeEmpty();
    }

    private static bool InheritsFrom(Type type, string baseTypeName)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == baseTypeName)
                return true;
        }

        return false;
    }
}
