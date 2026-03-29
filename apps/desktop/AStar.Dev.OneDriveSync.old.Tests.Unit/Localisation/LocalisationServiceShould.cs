using System.Globalization;
using AStar.Dev.OneDriveSync.old.Localisation;

namespace AStar.Dev.OneDriveSync.old.Tests.Unit.Localisation;

[TestSubject(typeof(LocalisationService))]
public class LocalisationServiceShould
{
    private readonly IStringResourceProvider _provider = Substitute.For<IStringResourceProvider>();
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("en-GB");
    private readonly LocalisationService _sut;

    public LocalisationServiceShould() => _sut = new LocalisationService(_provider, _culture);

    [Fact]
    public void ExposeCultureName()
        => _sut.Culture.ShouldBe("en-GB");

    [Fact]
    public void ReturnLocalisedString_ForKnownKey()
    {
        _ = _provider.GetString("MainWindow_Title", _culture).Returns("AStar Dev OneDrive Sync");

        _sut.GetString("MainWindow_Title").ShouldBe("AStar Dev OneDrive Sync");
    }

    [Fact]
    public void ReturnKey_WhenStringNotFound()
    {
        _ = _provider.GetString("MissingKey", _culture).Returns((string?)null);

        _sut.GetString("MissingKey").ShouldBe("MissingKey");
    }

    [Fact]
    public void PassCultureToProvider_WhenGettingString()
    {
        _ = _provider.GetString(Arg.Any<string>(), Arg.Any<CultureInfo>()).Returns("value");

        _ = _sut.GetString("AnyKey");

        _ = _provider.Received(1).GetString("AnyKey", _culture);
    }
}
