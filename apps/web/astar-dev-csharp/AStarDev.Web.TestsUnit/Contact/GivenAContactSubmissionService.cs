using AStarDev.FunctionalParadigm;
using AStarDev.Web.Contact;
using AStarDev.Web.TestsUnit.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AStarDev.Web.TestsUnit.Contact;

public class GivenAContactSubmissionService
{
    private readonly IContactRateLimiter rateLimiter = Substitute.For<IContactRateLimiter>();
    private readonly IContactEmailSender emailSender = Substitute.For<IContactEmailSender>();
    private readonly ILogger<ContactSubmissionService> logger = LoggedEvents.CreateEnabledLogger<ContactSubmissionService>();

    public GivenAContactSubmissionService()
    {
        rateLimiter.TryAcquire(Arg.Any<string>()).Returns(true);
        emailSender.SendAsync(Arg.Any<ContactMessage>(), Arg.Any<CancellationToken>()).Returns(Unit.Instance);
    }

    private ContactSubmissionService CreateSut() => new(rateLimiter, emailSender, logger);

    [Fact]
    public async Task when_the_honeypot_field_is_filled_in_then_the_outcome_is_success_and_no_email_is_sent()
    {
        var sut = CreateSut();

        var outcome = await sut.SubmitAsync("Ada Lovelace", "ada@example.com", "Interested in working together.", sendCopy: false, website: "https://spam.example", ipAddress: "1.2.3.4", TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ContactSubmissionSucceeded>();
        await emailSender.DidNotReceive().SendAsync(Arg.Any<ContactMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_rate_limit_is_exceeded_then_the_outcome_is_rate_limited()
    {
        rateLimiter.TryAcquire("1.2.3.4").Returns(false);
        var sut = CreateSut();

        var outcome = await sut.SubmitAsync("Ada Lovelace", "ada@example.com", "Interested in working together.", sendCopy: false, website: "", ipAddress: "1.2.3.4", TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ContactSubmissionRateLimited>();
        await emailSender.DidNotReceive().SendAsync(Arg.Any<ContactMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_field_is_invalid_then_the_outcome_carries_the_validation_errors()
    {
        var sut = CreateSut();

        var outcome = await sut.SubmitAsync("", "ada@example.com", "Interested in working together.", sendCopy: false, website: "", ipAddress: "1.2.3.4", TestContext.Current.CancellationToken);

        var validationFailed = outcome.ShouldBeOfType<ContactSubmissionValidationFailed>();
        validationFailed.Errors.ShouldContain(e => e.Property == "name");
    }

    [Fact]
    public async Task when_the_submission_is_valid_then_the_email_sender_is_called_and_the_outcome_is_success()
    {
        var sut = CreateSut();

        var outcome = await sut.SubmitAsync("Ada Lovelace", "ada@example.com", "Interested in working together.", sendCopy: true, website: "", ipAddress: "1.2.3.4", TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ContactSubmissionSucceeded>();
        await emailSender.Received(1).SendAsync(Arg.Is<ContactMessage>(m => m.Name == "Ada Lovelace" && m.SendCopy), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_sending_the_email_fails_then_the_outcome_carries_the_failure_message()
    {
        emailSender.SendAsync(Arg.Any<ContactMessage>(), Arg.Any<CancellationToken>()).Returns("Something went wrong.");
        var sut = CreateSut();

        var outcome = await sut.SubmitAsync("Ada Lovelace", "ada@example.com", "Interested in working together.", sendCopy: false, website: "", ipAddress: "1.2.3.4", TestContext.Current.CancellationToken);

        var sendFailed = outcome.ShouldBeOfType<ContactSubmissionSendFailed>();
        sendFailed.Message.ShouldBe("Something went wrong.");
    }

    [Theory]
    [InlineData("https://spam.example", true)]
    [InlineData("", false)]
    [InlineData("", true)]
    public async Task when_a_contact_message_is_submitted_then_no_log_value_contains_the_email_or_the_ip_address(string website, bool withinRateLimit)
    {
        rateLimiter.TryAcquire("1.2.3.4").Returns(withinRateLimit);
        var sut = CreateSut();

        await sut.SubmitAsync("Ada Lovelace", "ada@example.com", "Interested in working together.", sendCopy: false, website, ipAddress: "1.2.3.4", TestContext.Current.CancellationToken);

        logger.AllLoggedValues().ShouldNotBeEmpty();
        logger.AllLoggedValues().ShouldAllBe(value => !value.Contains("ada@example.com", StringComparison.OrdinalIgnoreCase) && !value.Contains("1.2.3.4", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_rate_limit_is_exceeded_twice_by_one_client_then_both_events_carry_the_same_short_client_hash()
    {
        rateLimiter.TryAcquire("1.2.3.4").Returns(false);
        var sut = CreateSut();

        await sut.SubmitAsync("Ada Lovelace", "ada@example.com", "Hello.", sendCopy: false, website: "", ipAddress: "1.2.3.4", TestContext.Current.CancellationToken);
        await sut.SubmitAsync("Ada Lovelace", "ada@example.com", "Hello.", sendCopy: false, website: "", ipAddress: "1.2.3.4", TestContext.Current.CancellationToken);

        var hashes = logger.AllLoggedValuesOf("RateLimitExceeded", "ClientHash");
        hashes.Count.ShouldBe(2);
        hashes.Distinct().ShouldHaveSingleItem().ShouldMatch("^[0-9a-f]{16}$");
    }
}
