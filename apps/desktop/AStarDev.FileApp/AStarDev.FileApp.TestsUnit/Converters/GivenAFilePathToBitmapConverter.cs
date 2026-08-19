using System.Globalization;
using AStar.Dev.File.App.Converters;

namespace AStar.Dev.File.App.TestsUnit.Converters;

public class GivenAFilePathToBitmapConverter
{
    private readonly FilePathToBitmapConverter _sut = new();

    [Fact]
    public void when_converting_a_null_value_then_returns_null()
        => _sut.Convert(null, typeof(object), null, CultureInfo.InvariantCulture).ShouldBeNull();

    [Fact]
    public void when_converting_a_non_string_value_then_returns_null()
        => _sut.Convert(42, typeof(object), null, CultureInfo.InvariantCulture).ShouldBeNull();

    [Fact]
    public void when_converting_a_path_that_does_not_exist_then_returns_null()
        => _sut.Convert("/nonexistent/path/image.jpg", typeof(object), null, CultureInfo.InvariantCulture).ShouldBeNull();

    [Fact]
    public void when_converting_an_empty_string_then_returns_null()
        => _sut.Convert(string.Empty, typeof(object), null, CultureInfo.InvariantCulture).ShouldBeNull();

    [Fact]
    public void when_converting_back_then_throws_not_supported_exception()
        => Should.Throw<NotSupportedException>(() => _sut.ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture));
}
