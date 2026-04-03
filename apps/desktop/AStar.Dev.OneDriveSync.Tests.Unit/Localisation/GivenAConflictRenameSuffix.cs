using System;
using System.Globalization;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Localisation;

public sealed class GivenAConflictRenameSuffix
{
    [Fact]
    public void when_formatted_with_different_cultures_then_output_is_identical()
    {
        var timestamp = new DateTimeOffset(2026, 3, 31, 12, 30, 45, TimeSpan.Zero);

        string withEnGb = timestamp.ToString("yyyy-MM-ddTHHmmssZ", CultureInfo.GetCultureInfo("en-GB"));
        string withDeDe = timestamp.ToString("yyyy-MM-ddTHHmmssZ", CultureInfo.GetCultureInfo("de-DE"));
        string withJaJp = timestamp.ToString("yyyy-MM-ddTHHmmssZ", CultureInfo.GetCultureInfo("ja-JP"));

        withEnGb.ShouldBe(withDeDe);
        withEnGb.ShouldBe(withJaJp);
    }

    [Fact]
    public void when_formatted_then_output_matches_expected_utc_pattern()
    {
        var timestamp = new DateTimeOffset(2026, 3, 31, 12, 30, 45, TimeSpan.Zero);

        string result = timestamp.ToString("yyyy-MM-ddTHHmmssZ", CultureInfo.InvariantCulture);

        result.ShouldBe("2026-03-31T123045Z");
    }
}
