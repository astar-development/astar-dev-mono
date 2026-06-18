using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Converters;
using AStar.Dev.OneDrive.Sync.Client.Home;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public sealed class GivenASyncStateToBadgeForegroundConverter
{
    private static readonly SyncStateToBadgeForegroundConverter Sut = SyncStateToBadgeForegroundConverter.Instance;

    [Theory]
    [InlineData(FolderSyncState.Synced,   "#27500A")]
    [InlineData(FolderSyncState.Syncing,  "#0C447C")]
    [InlineData(FolderSyncState.Included, "#0C447C")]
    [InlineData(FolderSyncState.Partial,  "#633806")]
    [InlineData(FolderSyncState.Conflict, "#633806")]
    [InlineData(FolderSyncState.Error,    "#791F1F")]
    [InlineData(FolderSyncState.Excluded, "#5F5E5A")]
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
        var first = SyncStateToBadgeForegroundConverter.Instance;
        var second = SyncStateToBadgeForegroundConverter.Instance;

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
    {
        _ = Should.Throw<NotSupportedException>(() =>
            Sut.ConvertBack(Colors.Transparent, typeof(FolderSyncState), null, CultureInfo.InvariantCulture));
    }
}
