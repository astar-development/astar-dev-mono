using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Onboarding;

public sealed class GivenWizardConverters
{
    public sealed class GivenABoolToAccentConverter
    {
        private static readonly BoolToAccentConverter Sut = BoolToAccentConverter.Instance;

        [Fact]
        public void when_value_is_true_then_active_color_is_returned()
        {
            var result = (Color)Sut.Convert(true, typeof(Color), null, CultureInfo.InvariantCulture);

            result.ShouldBe(Color.Parse("#185FA5"));
        }

        [Fact]
        public void when_value_is_false_then_inactive_color_is_returned()
        {
            var result = (Color)Sut.Convert(false, typeof(Color), null, CultureInfo.InvariantCulture);

            result.ShouldBe(Color.Parse("#D3D1C7"));
        }

        [Fact]
        public void when_value_is_null_then_inactive_color_is_returned()
        {
            var result = (Color)Sut.Convert(null, typeof(Color), null, CultureInfo.InvariantCulture);

            result.ShouldBe(Color.Parse("#D3D1C7"));
        }

        [Fact]
        public void when_instance_accessed_multiple_times_then_same_instance_is_returned()
        {
            var first = BoolToAccentConverter.Instance;
            var second = BoolToAccentConverter.Instance;

            first.ShouldBeSameAs(second);
        }

        [Fact]
        public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
        {
            _ = Should.Throw<NotSupportedException>(() =>
                Sut.ConvertBack(Colors.Transparent, typeof(bool), null, CultureInfo.InvariantCulture));
        }
    }

    public sealed class GivenAStringNotEmptyConverter
    {
        private static readonly StringNotEmptyConverter Sut = StringNotEmptyConverter.Instance;

        [Fact]
        public void when_value_is_non_empty_string_then_result_is_true()
        {
            object result = Sut.Convert("hello", typeof(bool), null, CultureInfo.InvariantCulture);

            result.ShouldBe(true);
        }

        [Fact]
        public void when_value_is_empty_string_then_result_is_false()
        {
            object result = Sut.Convert(string.Empty, typeof(bool), null, CultureInfo.InvariantCulture);

            result.ShouldBe(false);
        }

        [Fact]
        public void when_value_is_null_then_result_is_false()
        {
            object result = Sut.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

            result.ShouldBe(false);
        }

        [Fact]
        public void when_value_is_whitespace_then_result_is_true()
        {
            object result = Sut.Convert("   ", typeof(bool), null, CultureInfo.InvariantCulture);

            result.ShouldBe(true);
        }

        [Fact]
        public void when_instance_accessed_multiple_times_then_same_instance_is_returned()
        {
            var first = StringNotEmptyConverter.Instance;
            var second = StringNotEmptyConverter.Instance;

            first.ShouldBeSameAs(second);
        }

        [Fact]
        public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
        {
            _ = Should.Throw<NotSupportedException>(() =>
                Sut.ConvertBack(true, typeof(string), null, CultureInfo.InvariantCulture));
        }
    }
}
