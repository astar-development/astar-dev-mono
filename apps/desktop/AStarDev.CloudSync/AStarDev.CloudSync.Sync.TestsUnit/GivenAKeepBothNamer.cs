using System.Globalization;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAKeepBothNamer
{
    private static readonly DateTimeOffset Moment = new(2026, 11, 14, 9, 5, 7, TimeSpan.Zero);

    private readonly KeepBothNamer namer = new();

    [Fact]
    public void when_a_file_with_an_extension_is_named_then_the_stamp_goes_before_the_extension()
    {
        var name = namer.Name("report.docx", Moment, []);

        name.ShouldBe("report-(2026-11-14T090507Z).docx");
    }

    [Fact]
    public void when_a_file_has_no_extension_then_the_stamp_is_at_the_end()
    {
        var name = namer.Name("README", Moment, []);

        name.ShouldBe("README-(2026-11-14T090507Z)");
    }

    [Fact]
    public void when_a_file_has_several_dots_then_only_the_last_extension_stays_at_the_end()
    {
        var name = namer.Name("archive.tar.gz", Moment, []);

        name.ShouldBe("archive.tar-(2026-11-14T090507Z).gz");
    }

    [Fact]
    public void when_a_file_name_starts_with_a_dot_and_has_no_other_dot_then_the_stamp_is_at_the_end()
    {
        var name = namer.Name(".gitignore", Moment, []);

        name.ShouldBe(".gitignore-(2026-11-14T090507Z)");
    }

    [Fact]
    public void when_the_moment_has_an_offset_then_the_stamp_is_in_utc()
    {
        var local = new DateTimeOffset(2026, 11, 14, 11, 5, 7, TimeSpan.FromHours(2));

        var name = namer.Name("a.txt", local, []);

        name.ShouldBe("a-(2026-11-14T090507Z).txt");
    }

    [Fact]
    public void when_the_current_culture_is_german_then_the_name_is_the_same()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        var name = namer.Name("a.txt", Moment, []);

        CultureInfo.CurrentCulture = previous;
        name.ShouldBe("a-(2026-11-14T090507Z).txt");
    }

    [Fact]
    public void when_the_name_is_taken_then_a_counter_of_two_is_added()
    {
        var name = namer.Name("a.txt", Moment, ["a-(2026-11-14T090507Z).txt"]);

        name.ShouldBe("a-(2026-11-14T090507Z)-2.txt");
    }

    [Fact]
    public void when_the_counted_name_is_taken_too_then_the_next_counter_is_used()
    {
        var name = namer.Name("a.txt", Moment, ["a-(2026-11-14T090507Z).txt", "a-(2026-11-14T090507Z)-2.txt"]);

        name.ShouldBe("a-(2026-11-14T090507Z)-3.txt");
    }

    [Fact]
    public void when_the_taken_name_differs_only_in_case_then_it_is_still_a_clash()
    {
        var name = namer.Name("a.txt", Moment, ["A-(2026-11-14T090507Z).TXT"]);

        name.ShouldBe("a-(2026-11-14T090507Z)-2.txt");
    }

    [Fact]
    public void when_the_original_is_empty_then_a_stamped_name_is_still_returned()
    {
        var name = namer.Name(string.Empty, Moment, []);

        name.ShouldBe("-(2026-11-14T090507Z)");
    }
}
