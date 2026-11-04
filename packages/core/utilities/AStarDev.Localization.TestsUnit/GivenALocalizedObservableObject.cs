using System.Globalization;

namespace AStarDev.Localization.TestsUnit;

public sealed class GivenALocalizedObservableObject
{
    [Fact]
    public void when_the_culture_changes_then_property_changed_is_raised_for_all_properties()
    {
        var localization = Substitute.For<ILocalizationService>();
        var viewModel = new LocalizedTestViewModel(localization);
        var changedPropertyNames = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changedPropertyNames.Add(args.PropertyName);

        localization.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(localization, new CultureInfo("en-US"));

        changedPropertyNames.ShouldBe([string.Empty]);
    }

    [Fact]
    public void when_created_then_the_localization_service_is_exposed_to_derived_classes()
    {
        var localization = Substitute.For<ILocalizationService>();

        var viewModel = new LocalizedTestViewModel(localization);

        viewModel.ExposedLocalization.ShouldBeSameAs(localization);
    }

    private sealed class LocalizedTestViewModel(ILocalizationService localization) : LocalizedObservableObject(localization)
    {
        public ILocalizationService ExposedLocalization => Localization;
    }
}
