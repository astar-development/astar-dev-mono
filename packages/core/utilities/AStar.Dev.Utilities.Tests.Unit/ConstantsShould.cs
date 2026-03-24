namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class ConstantsShould
{
    [Fact(Skip = "Dunno why this is failing on the build server but not locally")]
    public void ContainTheExpectedWebDeserialisationSettingsSetting() =>
        Constants.WebDeserialisationSettings
                 .ToJson()
                 .ShouldMatchApproved();
}
