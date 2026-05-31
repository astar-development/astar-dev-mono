using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Onboarding;

public sealed class GivenABoolToCollapseExpandConverter
{
    private readonly ILocalizationService localizationService = Substitute.For<ILocalizationService>();

    private BoolToCollapseExpandConverter CreateSut() => new(localizationService);

    [Fact]
    public void when_value_is_true_then_returns_collapse_key_value()
    {
        localizationService.GetLocal("Conflict.Collapse").Returns("Collapse");
        var sut = CreateSut();

        object result = sut.Convert(true, typeof(string), null, CultureInfo.CurrentCulture);

        result.ShouldBe("Collapse");
    }

    [Fact]
    public void when_value_is_false_then_returns_resolve_key_value()
    {
        localizationService.GetLocal("Conflict.Resolve").Returns("Resolve");
        var sut = CreateSut();

        object result = sut.Convert(false, typeof(string), null, CultureInfo.CurrentCulture);

        result.ShouldBe("Resolve");
    }

    [Fact]
    public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
    {
        var sut = CreateSut();

        _ = Should.Throw<NotSupportedException>(() =>
            sut.ConvertBack("Collapse", typeof(bool), null, CultureInfo.CurrentCulture));
    }
}
