using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public class GivenAnMsalIdentityLogger
{
    private readonly RecordingLogger<MsalIdentityLogger> logger = new();

    [Theory]
    [InlineData(EventLogLevel.Critical, true)]
    [InlineData(EventLogLevel.Error, true)]
    [InlineData(EventLogLevel.Warning, true)]
    [InlineData(EventLogLevel.Informational, false)]
    [InlineData(EventLogLevel.Verbose, false)]
    public void when_msal_asks_whether_a_level_is_logged_then_only_warnings_and_above_are(EventLogLevel level, bool expected) => new MsalIdentityLogger(logger).IsEnabled(level).ShouldBe(expected);

    [Theory]
    [InlineData(EventLogLevel.Critical, LogLevel.Critical)]
    [InlineData(EventLogLevel.Error, LogLevel.Error)]
    [InlineData(EventLogLevel.Warning, LogLevel.Warning)]
    public void when_msal_logs_then_the_message_is_forwarded_at_the_matching_level(EventLogLevel msalLevel, LogLevel expected)
    {
        new MsalIdentityLogger(logger).Log(new LogEntry { EventLogLevel = msalLevel, Message = "Token cache could not be read." });

        logger.Entries.ShouldHaveSingleItem().Level.ShouldBe(expected);
        logger.ValuesOf("MsalMessage")["MsalMessage"].ShouldBe("Token cache could not be read.");
    }

    [Fact]
    public void when_msal_logs_below_warning_then_nothing_is_forwarded()
    {
        new MsalIdentityLogger(logger).Log(new LogEntry { EventLogLevel = EventLogLevel.Verbose, Message = "Acquiring token." });

        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void when_msal_logs_text_with_an_email_or_guid_then_both_are_redacted_and_the_rest_is_kept()
    {
        new MsalIdentityLogger(logger).Log(new LogEntry { EventLogLevel = EventLogLevel.Error, Message = "AADSTS50034: The user account Captain.Flint@Example.com does not exist. Trace ID: 0b1c2d3e-4f50-4617-8293-a4b5c6d7e8f9" });

        logger.ValuesOf("MsalMessage")["MsalMessage"].ShouldBe("AADSTS50034: The user account [redacted] does not exist. Trace ID: [redacted]");
    }
}
