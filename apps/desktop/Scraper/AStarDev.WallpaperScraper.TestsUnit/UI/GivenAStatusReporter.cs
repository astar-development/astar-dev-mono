using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.UI;

namespace AStarDev.WallpaperScraper.TestsUnit.UI;

public sealed class GivenAStatusReporter
{
    private readonly StatusReporter reporter = TestStatusReporter.Create();

    [Fact]
    public void when_messages_are_appended_then_the_text_lists_them_oldest_first()
    {
        reporter.Append("first");
        reporter.Append("second");

        reporter.Text.ShouldBe($"{TestStatusReporter.Timestamp} first{Environment.NewLine}{TestStatusReporter.Timestamp} second");
    }

    [Fact]
    public void when_the_text_has_not_been_read_then_a_burst_of_messages_raises_refresh_required_once()
    {
        var raised = 0;
        reporter.RefreshRequired += (_, _) => raised++;

        reporter.Append("first");
        reporter.Append("second");
        reporter.Append("third");

        raised.ShouldBe(1);
    }

    [Fact]
    public void when_the_text_is_read_then_the_next_message_raises_refresh_required_again()
    {
        var raised = 0;
        reporter.RefreshRequired += (_, _) => raised++;
        reporter.Append("first");
        _ = reporter.Text;

        reporter.Append("second");

        raised.ShouldBe(2);
    }

    [Fact]
    public void when_an_error_is_reported_then_the_message_and_the_exception_message_are_appended()
    {
        reporter.Error("Unable to do the thing.", new InvalidOperationException("it broke"));

        reporter.Text.ShouldBe($"{TestStatusReporter.Timestamp} Unable to do the thing. it broke");
    }

    [Fact]
    public void when_an_error_has_an_inner_exception_then_its_message_is_appended_too()
    {
        reporter.Error("Unable to save.", new InvalidOperationException("An error occurred while saving the entity changes.", new IOException("UNIQUE constraint failed: FileDetail.FileHandle")));

        reporter.Text.ShouldBe($"{TestStatusReporter.Timestamp} Unable to save. An error occurred while saving the entity changes. Caused by: UNIQUE constraint failed: FileDetail.FileHandle");
    }

    [Fact]
    public void when_a_message_is_appended_then_it_is_prefixed_with_the_time_as_hh_mm_ss()
    {
        reporter.Append("hello");

        reporter.Text.ShouldBe($"{TestStatusReporter.Timestamp} hello");
    }

    [Fact]
    public void when_the_scraper_backs_off_then_a_message_states_the_delay()
    {
        var notifier = new RateLimitBackoffNotifier();
        var backoffReporter = TestStatusReporter.Create(notifier);

        notifier.NotifyBackingOff(TimeSpan.FromSeconds(30));

        backoffReporter.Text.ShouldBe($"{TestStatusReporter.Timestamp} Rate limited by Wallhaven (429) - waiting 30s before retrying.");
    }

    [Fact]
    public void when_the_back_off_delay_has_a_fraction_of_a_second_then_it_is_rounded_up()
    {
        var notifier = new RateLimitBackoffNotifier();
        var backoffReporter = TestStatusReporter.Create(notifier);

        notifier.NotifyBackingOff(TimeSpan.FromSeconds(1.2));

        backoffReporter.Text.ShouldBe($"{TestStatusReporter.Timestamp} Rate limited by Wallhaven (429) - waiting 2s before retrying.");
    }
}
