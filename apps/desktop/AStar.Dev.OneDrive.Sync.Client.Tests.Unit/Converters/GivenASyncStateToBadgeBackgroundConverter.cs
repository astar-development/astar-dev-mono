using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Converters;
using AStar.Dev.OneDrive.Sync.Client.Home;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public sealed class GivenASyncStateToBadgeBackgroundConverter
{
    private static readonly SyncStateToBadgeBackgroundConverter Sut = SyncStateToBadgeBackgroundConverter.Instance;

    [Theory]
    [InlineData(FolderSyncState.Synced,   "#EAF3DE")]
    [InlineData(FolderSyncState.Syncing,  "#E6F1FB")]
    [InlineData(FolderSyncState.Included, "#E6F1FB")]
    [InlineData(FolderSyncState.Partial,  "#FAEEDA")]
    [InlineData(FolderSyncState.Conflict, "#FAEEDA")]
    [InlineData(FolderSyncState.Error,    "#FCEBEB")]
    [InlineData(FolderSyncState.Excluded, "#F1EFE8")]
    public void when_folder_sync_state_is_provided_then_correct_color_is_returned(FolderSyncState state, string expectedHex)
    {
        var result = (Color)Sut.Convert(state, typeof(Color), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Color.Parse(expectedHex));
    }

    [Fact]
    public void when_value_is_null_then_transparent_is_returned()
    {
        object result = Sut.Convert(null, typeof(Color), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Colors.Transparent);
    }

    [Fact]
    public void when_value_is_non_folder_sync_state_then_transparent_is_returned()
    {
        object result = Sut.Convert("not a state", typeof(Color), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Colors.Transparent);
    }

    [Fact]
    public void when_instance_accessed_multiple_times_then_same_instance_is_returned()
    {
        var first = SyncStateToBadgeBackgroundConverter.Instance;
        var second = SyncStateToBadgeBackgroundConverter.Instance;

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
    {
        _ = Should.Throw<NotSupportedException>(() =>
            Sut.ConvertBack(Colors.Transparent, typeof(FolderSyncState), null, CultureInfo.InvariantCulture));
    }
}
