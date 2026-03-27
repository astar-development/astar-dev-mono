using AStar.Dev.OneDriveSync.Infrastructure.Persistence.Converters;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Persistence.Converters;

/// <summary>
///     Verifies AC DB-01 (unit test): "<see cref="DateTimeOffsetToUnixMillisecondsConverter" />
///     round-trip preserves UTC offset" (S002 — DateTimeOffset Storage).
///
///     All <see cref="DateTimeOffset" /> properties in the model are stored as Unix
///     milliseconds (<see langword="long" />) and reconstructed on read.  The converter
///     must faithfully preserve the UTC instant (to millisecond precision), regardless
///     of the original time-zone offset.
/// </summary>
public sealed class DateTimeOffsetToUnixMillisecondsConverterShould
{
    private static readonly DateTimeOffset UtcEpoch =
        new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // ──────────────────────────────────────────────────────────────────────────
    // ConvertToProvider (DateTimeOffset → long)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertEpochToZeroMilliseconds()
    {
        var sut    = new DateTimeOffsetToUnixMillisecondsConverter();
        var result = (long)sut.ConvertToProvider(UtcEpoch)!;
        result.ShouldBe(0L);
    }

    [Theory]
    [InlineData( 5,  0)]   // UTC+5
    [InlineData(-8,  0)]   // UTC-8
    [InlineData( 5, 30)]   // UTC+5:30 (India)
    [InlineData( 0,  0)]   // UTC
    public void ConvertToProviderReturnsTheSameUnixTimestampRegardlessOfOffset(int offsetHours, int offsetMinutes)
    {
        var offset  = TimeSpan.FromHours(offsetHours) + TimeSpan.FromMinutes(offsetMinutes);
        var dto     = new DateTimeOffset(2024, 6, 15, 12, 0, 0, offset);
        var sut     = new DateTimeOffsetToUnixMillisecondsConverter();

        var result  = (long)sut.ConvertToProvider(dto)!;

        result.ShouldBe(dto.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void ConvertToProviderPreservesMillisecondPrecision()
    {
        var dto     = new DateTimeOffset(2024, 3, 15, 10, 30, 45, 123, TimeSpan.Zero);
        var sut     = new DateTimeOffsetToUnixMillisecondsConverter();

        var result  = (long)sut.ConvertToProvider(dto)!;

        result.ShouldBe(dto.ToUnixTimeMilliseconds());
        (result % 1000).ShouldBe(123L);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ConvertFromProvider (long → DateTimeOffset)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertZeroMillisecondsToEpoch()
    {
        var sut    = new DateTimeOffsetToUnixMillisecondsConverter();
        var result = (DateTimeOffset)sut.ConvertFromProvider(0L)!;
        result.ShouldBe(UtcEpoch);
    }

    [Fact]
    public void ConvertFromProviderReturnsUtcDateTimeOffset()
    {
        var originalMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var sut        = new DateTimeOffsetToUnixMillisecondsConverter();

        var result     = (DateTimeOffset)sut.ConvertFromProvider(originalMs)!;

        result.Offset.ShouldBe(TimeSpan.Zero);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Round-trip (AC DB-01 — spec-mandated)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PreserveUtcInstantOnRoundTrip()
    {
        // This is the test explicitly mandated by S002:
        // "DateTimeOffset round-trip via value converter preserves UTC offset"
        var original = new DateTimeOffset(2025, 11, 28, 14, 45, 30, 500, TimeSpan.FromHours(3));
        var sut      = new DateTimeOffsetToUnixMillisecondsConverter();

        var stored      = (long)sut.ConvertToProvider(original)!;
        var roundTripped = (DateTimeOffset)sut.ConvertFromProvider(stored)!;

        // The UTC instant must be identical to millisecond precision
        roundTripped.ToUnixTimeMilliseconds().ShouldBe(original.ToUnixTimeMilliseconds());
        roundTripped.UtcDateTime.ShouldBe(original.UtcDateTime);
    }

    [Theory]
    [InlineData(  0)]                           // epoch
    [InlineData(  1_000)]                       // 1 second
    [InlineData(  86_400_000)]                  // 1 day
    [InlineData(  1_700_000_000_000L)]          // ~2023-11-14
    [InlineData( -1_000)]                       // 1 second before epoch
    public void RoundTripPreservesArbitraryUnixMillisecondValues(long ms)
    {
        var sut      = new DateTimeOffsetToUnixMillisecondsConverter();
        var original = DateTimeOffset.FromUnixTimeMilliseconds(ms);

        var stored      = (long)sut.ConvertToProvider(original)!;
        var roundTripped = (DateTimeOffset)sut.ConvertFromProvider(stored)!;

        roundTripped.ToUnixTimeMilliseconds().ShouldBe(ms);
    }
}
