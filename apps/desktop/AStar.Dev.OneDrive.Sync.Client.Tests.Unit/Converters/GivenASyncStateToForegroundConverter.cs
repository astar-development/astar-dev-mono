using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Converters;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public sealed class GivenASyncStateToForegroundConverter
{
    private static readonly SyncStateToForegroundConverter Sut = SyncStateToForegroundConverter.Instance;

    [Theory]
    [InlineData(SyncState.Syncing,  "#185FA5")]
    [InlineData(SyncState.Pending,  "#BA7517")]
    [InlineData(SyncState.Conflict, "#E24B4A")]
    [InlineData(SyncState.Error,    "#E24B4A")]
    [InlineData(SyncState.Idle,     "#1D9E75")]
    [InlineData(SyncState.Completed,"#1D9E75")]
    public void when_sync_state_is_provided_then_correct_brush_color_is_returned(SyncState state, string expectedHex)
    {
        var result = (SolidColorBrush)Sut.Convert(state, typeof(IBrush), null, CultureInfo.InvariantCulture)!;

        result.Color.ShouldBe(Color.Parse(expectedHex));
    }

    [Fact]
    public void when_value_is_null_then_transparent_brush_is_returned()
    {
        object? result = Sut.Convert(null, typeof(IBrush), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Brushes.Transparent);
    }

    [Fact]
    public void when_value_is_non_sync_state_then_transparent_brush_is_returned()
    {
        object? result = Sut.Convert("not a state", typeof(IBrush), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Brushes.Transparent);
    }

    [Fact]
    public void when_instance_accessed_multiple_times_then_same_instance_is_returned()
    {
        var first = SyncStateToForegroundConverter.Instance;
        var second = SyncStateToForegroundConverter.Instance;

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
    {
        _ = Should.Throw<NotSupportedException>(() =>
            Sut.ConvertBack(Brushes.Transparent, typeof(SyncState), null, CultureInfo.InvariantCulture));
    }
}
