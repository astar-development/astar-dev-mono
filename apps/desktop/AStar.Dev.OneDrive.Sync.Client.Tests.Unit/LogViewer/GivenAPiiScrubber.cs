using AStar.Dev.OneDrive.Sync.Client.LogViewer;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.LogViewer;

public sealed class GivenAPiiScrubber
{
    [Fact]
    public void when_message_contains_no_email_then_message_is_returned_unchanged()
    {
        string message = "SyncAccountAsync for account-123";

        string result = PiiScrubber.Scrub(message);

        result.ShouldBe("SyncAccountAsync for account-123");
    }

    [Fact]
    public void when_message_contains_one_email_then_email_is_replaced_with_placeholder()
    {
        string message = "[SyncService] SyncAccountAsync for user@outlook.com";

        string result = PiiScrubber.Scrub(message);

        result.ShouldBe("[SyncService] SyncAccountAsync for [email redacted]");
    }

    [Fact]
    public void when_message_contains_multiple_emails_then_all_are_replaced()
    {
        string message = "Syncing alice@outlook.com and bob@example.com";

        string result = PiiScrubber.Scrub(message);

        result.ShouldBe("Syncing [email redacted] and [email redacted]");
    }

    [Fact]
    public void when_message_is_empty_then_empty_string_is_returned()
    {
        string result = PiiScrubber.Scrub(string.Empty);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_message_contains_email_with_subdomain_then_email_is_redacted()
    {
        string message = "Authenticated as user@mail.subdomain.example.co.uk";

        string result = PiiScrubber.Scrub(message);

        result.ShouldBe("Authenticated as [email redacted]");
    }
}
