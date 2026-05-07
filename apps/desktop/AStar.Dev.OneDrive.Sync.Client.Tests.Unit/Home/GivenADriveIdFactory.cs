using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Home;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenADriveIdFactory
{
    [Fact]
    public void when_create_is_called_with_a_value_then_some_is_returned()
    {
        var result = DriveIdFactory.Create("drive-abc");

        result.ShouldBeOfType<Option<DriveId>.Some>();
    }

    [Fact]
    public void when_create_is_called_with_a_value_then_value_is_wrapped()
    {
        var result = DriveIdFactory.Create("drive-abc");

        result.Match(id => id.Value, () => string.Empty).ShouldBe("drive-abc");
    }

    [Fact]
    public void when_create_is_called_with_null_then_none_is_returned()
    {
        var result = DriveIdFactory.Create(null!);

        result.ShouldBeOfType<Option<DriveId>.None>();
    }

    [Fact]
    public void when_create_is_called_with_empty_string_then_none_is_returned()
    {
        var result = DriveIdFactory.Create(string.Empty);

        result.ShouldBeOfType<Option<DriveId>.None>();
    }

    [Fact]
    public void when_empty_is_accessed_then_none_is_returned()
    {
        DriveIdFactory.Empty.ShouldBeOfType<Option<DriveId>.None>();
    }
}
