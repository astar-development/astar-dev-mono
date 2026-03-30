using AStar.Dev.OneDriveSync.Infrastructure.Persistence.Converters;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Persistence.Converters;

public sealed class GivenADateTimeOffsetValue
{
    private static readonly DateTimeOffset                            UtcEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffsetToUnixMillisecondsConverter Sut      = new();

    [Fact]
    public void when_the_epoch_is_converted_then_zero_milliseconds_is_returned() =>
        ((long)Sut.ConvertToProvider(UtcEpoch)!).ShouldBe(0L);

    [Theory]
    [InlineData( 5,  0)]
    [InlineData(-8,  0)]
    [InlineData( 5, 30)]
    [InlineData( 0,  0)]
    public void when_converted_to_provider_format_then_the_unix_timestamp_is_independent_of_the_utc_offset(int offsetHours, int offsetMinutes)
    {
        var dto = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(offsetHours) + TimeSpan.FromMinutes(offsetMinutes));

        long result = (long)Sut.ConvertToProvider(dto)!;

        result.ShouldBe(dto.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void when_converted_to_provider_format_then_millisecond_precision_is_preserved()
    {
        var dto = new DateTimeOffset(2024, 3, 15, 10, 30, 45, 123, TimeSpan.Zero);

        long result = (long)Sut.ConvertToProvider(dto)!;

        result.ShouldBe(dto.ToUnixTimeMilliseconds());
        (result % 1000).ShouldBe(123L);
    }

    [Fact]
    public void when_zero_milliseconds_is_converted_then_the_epoch_is_returned() =>
        ((DateTimeOffset)Sut.ConvertFromProvider(0L)!).ShouldBe(UtcEpoch);

    [Fact]
    public void when_converted_from_provider_format_then_the_result_has_utc_offset()
    {
        var result = (DateTimeOffset)Sut.ConvertFromProvider(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())!;

        result.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void when_stored_and_reloaded_then_the_utc_instant_is_preserved_to_millisecond_precision()
    {
        var original = new DateTimeOffset(2025, 11, 28, 14, 45, 30, 500, TimeSpan.FromHours(3));

        var roundTripped = (DateTimeOffset)Sut.ConvertFromProvider((long)Sut.ConvertToProvider(original)!)!;

        roundTripped.ToUnixTimeMilliseconds().ShouldBe(original.ToUnixTimeMilliseconds());
        roundTripped.UtcDateTime.ShouldBe(original.UtcDateTime);
    }

    [Theory]
    [InlineData(              0L)]
    [InlineData(          1_000L)]
    [InlineData(     86_400_000L)]
    [InlineData(1_700_000_000_000L)]
    [InlineData(         -1_000L)]
    public void when_round_tripped_then_the_unix_millisecond_value_is_preserved(long ms)
    {
        var original = DateTimeOffset.FromUnixTimeMilliseconds(ms);

        var roundTripped = (DateTimeOffset)Sut.ConvertFromProvider((long)Sut.ConvertToProvider(original)!)!;

        roundTripped.ToUnixTimeMilliseconds().ShouldBe(ms);
    }
}
