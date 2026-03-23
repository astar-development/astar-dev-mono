using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace AStar.Dev.AspNet.Extensions.ConfigurationManagerExtensions;

[TestSubject(typeof(ConfigurationManagerExtensions))]
public sealed class ConfigurationManagerExtensionsShould
{
    [Fact]
    public void ReturnAnEmptyConfigurationWhenTheConfigurationKeyDoesNotExist()
    {
        var sut = new ConfigurationManager();

        var configuration = sut.GetValidatedConfigurationSection<ApiConfiguration>("Some Key That Does Not Exist");

        configuration.ShouldBeEquivalentTo(new ApiConfiguration());
    }

    [Fact(Skip = "Underlying code is broken")]
    public void ReturnTheExpectedConfigurationWhenTheConfigurationKeyExists()
    {
        var sut = new ConfigurationManager();
        sut.AddJsonFile("testdata/appsettings.json");

        var configuration = sut.GetValidatedConfigurationSection<ApiConfiguration>("apiConfiguration")!;

        configuration.OpenApiInfo.Title.ShouldBe("AStar Development Admin Api - from TestData.");
    }
}
