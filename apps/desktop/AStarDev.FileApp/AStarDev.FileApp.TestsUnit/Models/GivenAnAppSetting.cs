using AStar.Dev.File.App.Models;

namespace AStar.Dev.File.App.TestsUnit.Models;

public class GivenAnAppSetting
{
    [Fact]
    public void when_constructed_with_key_and_value_then_properties_round_trip()
    {
        var sut = new AppSetting { Id = 7, Key = "SelectedFolderPath", Value = "/data/photos" };

        sut.Id.ShouldBe(7);
        sut.Key.ShouldBe("SelectedFolderPath");
        sut.Value.ShouldBe("/data/photos");
    }
}
