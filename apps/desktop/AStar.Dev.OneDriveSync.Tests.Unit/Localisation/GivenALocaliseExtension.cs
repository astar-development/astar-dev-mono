using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Localisation;

public sealed class GivenALocaliseExtension
{
    [Fact]
    public void when_service_locator_instance_is_null_then_provide_value_returns_the_key()
    {
        LocalisationServiceLocator.Instance = null;
        var sut = new LocaliseExtension("Settings_Heading");

        object result = sut.ProvideValue(serviceProvider: null!);

        result.ShouldBe("Settings_Heading");
    }
}
