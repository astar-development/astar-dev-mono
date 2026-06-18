using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAVersionInfoFactory
{
    [Fact]
    public void when_creating_with_some_etag_then_etag_is_set()
    {
        var info = VersionInfoFactory.Create(Option.Some("etag-abc"), Option.None<string>());

        info.ETag.ShouldBe(Option.Some("etag-abc"));
    }

    [Fact]
    public void when_creating_with_some_ctag_then_ctag_is_set()
    {
        var info = VersionInfoFactory.Create(Option.None<string>(), Option.Some("ctag-xyz"));

        info.CTag.ShouldBe(Option.Some("ctag-xyz"));
    }

    [Fact]
    public void when_creating_with_none_values_then_both_are_none()
    {
        var info = VersionInfoFactory.Create(Option.None<string>(), Option.None<string>());

        info.ETag.ShouldBe(Option.None<string>());
        info.CTag.ShouldBe(Option.None<string>());
    }

    [Fact]
    public void when_creating_with_both_some_then_both_are_preserved()
    {
        var info = VersionInfoFactory.Create(Option.Some("etag-1"), Option.Some("ctag-2"));

        info.ETag.ShouldBe(Option.Some("etag-1"));
        info.CTag.ShouldBe(Option.Some("ctag-2"));
    }
}
