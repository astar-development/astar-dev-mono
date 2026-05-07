using AStar.Dev.OneDrive.Sync.Client.Home;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenADriveId
{
    [Fact]
    public void when_constructed_with_value_then_value_is_stored()
    {
        var sut = new DriveId("drive-abc");

        sut.Value.ShouldBe("drive-abc");
    }

    [Fact]
    public void when_two_instances_have_same_value_then_they_are_equal()
    {
        var first  = new DriveId("drive-abc");
        var second = new DriveId("drive-abc");

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_values_then_they_are_not_equal()
    {
        var first  = new DriveId("drive-abc");
        var second = new DriveId("drive-xyz");

        first.ShouldNotBe(second);
    }
}
