using AStar.Dev.FunctionalParadigm;
using Azure;
using Azure.Communication.Email;
using Fab4Kids.Web.Newsletter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Newsletter;

public class GivenAnAzureNewsletterEmailSender
{
    private readonly EmailClient emailClient = Substitute.For<EmailClient>();
    private readonly ILogger<AzureNewsletterEmailSender> logger = Substitute.For<ILogger<AzureNewsletterEmailSender>>();

    private static readonly NewsletterSubscriber Subscriber = NewsletterSubscriberFactory.Create("ada@example.com", DateTimeOffset.UtcNow);

    public GivenAnAzureNewsletterEmailSender() =>
        emailClient.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).Returns((EmailSendOperation)null!);

    private AzureNewsletterEmailSender CreateSut(EmailClient? client, NewsletterOptions options) => new(Options.Create(options), logger, client);

    private static NewsletterOptions ConfiguredOptions() => new() { FromAddress = "noreply@fab4kids.co.uk" };

    [Fact]
    public async Task when_the_email_client_is_not_configured_then_an_error_is_returned()
    {
        var sut = CreateSut(null, ConfiguredOptions());

        var result = await sut.SendAsync(Subscriber, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong sending your confirmation email.");
    }

    [Fact]
    public async Task when_the_from_address_is_missing_then_an_error_is_returned()
    {
        var sut = CreateSut(emailClient, new NewsletterOptions());

        var result = await sut.SendAsync(Subscriber, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong sending your confirmation email.");
    }

    [Fact]
    public async Task when_sending_succeeds_then_the_confirmation_email_is_sent_to_the_subscriber()
    {
        var sut = CreateSut(emailClient, ConfiguredOptions());

        await sut.SendAsync(Subscriber, TestContext.Current.CancellationToken);

        await emailClient.Received(1).SendAsync(Arg.Any<WaitUntil>(), Arg.Is<EmailMessage>(m => m.Recipients.To[0].Address == "ada@example.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_sending_succeeds_then_a_success_result_is_returned()
    {
        var sut = CreateSut(emailClient, ConfiguredOptions());

        var result = await sut.SendAsync(Subscriber, TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_email_client_throws_then_an_error_is_returned()
    {
        emailClient.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task<EmailSendOperation>>(_ => throw new InvalidOperationException("ACS unavailable"));
        var sut = CreateSut(emailClient, ConfiguredOptions());

        var result = await sut.SendAsync(Subscriber, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong sending your confirmation email.");
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_the_email_client_is_never_called()
    {
        var sut = CreateSut(emailClient, ConfiguredOptions());
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.SendAsync(Subscriber, cancellationTokenSource.Token));

        await emailClient.DidNotReceive().SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
