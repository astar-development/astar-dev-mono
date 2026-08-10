using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.Contact;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AStarDev.Web.Tests.Unit.Contact;

public class GivenAnAzureContactEmailSender
{
    private readonly EmailClient emailClient = Substitute.For<EmailClient>();
    private readonly ILogger<AzureContactEmailSender> logger = Substitute.For<ILogger<AzureContactEmailSender>>();

    private static readonly ContactMessage Message = ContactMessageFactory.Create("Ada Lovelace", "ada@example.com", "Interested in working together.", sendCopy: false);

    public GivenAnAzureContactEmailSender() =>
        emailClient.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).Returns((EmailSendOperation)null!);

    private AzureContactEmailSender CreateSut(EmailClient? client, ContactFormOptions options) =>
        new(Options.Create(options), logger, client);

    private static ContactFormOptions ConfiguredOptions() => new() { FromAddress = "noreply@astardevelopment.co.uk", ToAddress = "owner@astardevelopment.co.uk" };

    [Fact]
    public async Task when_the_email_client_is_not_configured_then_an_error_is_returned()
    {
        var sut = CreateSut(null, ConfiguredOptions());

        var result = await sut.SendAsync(Message, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong. Please try again later.");
    }

    [Fact]
    public async Task when_the_from_address_is_missing_then_an_error_is_returned()
    {
        var sut = CreateSut(emailClient, new ContactFormOptions { ToAddress = "owner@astardevelopment.co.uk" });

        var result = await sut.SendAsync(Message, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong. Please try again later.");
    }

    [Fact]
    public async Task when_send_copy_is_false_then_only_the_owner_notification_is_sent()
    {
        var sut = CreateSut(emailClient, ConfiguredOptions());

        await sut.SendAsync(Message, TestContext.Current.CancellationToken);

        await emailClient.Received(1).SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_send_copy_is_true_then_the_owner_notification_and_a_copy_are_sent()
    {
        var messageWithCopy = ContactMessageFactory.Create("Ada Lovelace", "ada@example.com", "Interested in working together.", sendCopy: true);
        var sut = CreateSut(emailClient, ConfiguredOptions());

        await sut.SendAsync(messageWithCopy, TestContext.Current.CancellationToken);

        await emailClient.Received(2).SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_sending_succeeds_then_a_success_result_is_returned()
    {
        var sut = CreateSut(emailClient, ConfiguredOptions());

        var result = await sut.SendAsync(Message, TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_email_client_throws_then_an_error_naming_the_owner_address_is_returned()
    {
        emailClient.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task<EmailSendOperation>>(_ => throw new InvalidOperationException("ACS unavailable"));
        var sut = CreateSut(emailClient, ConfiguredOptions());

        var result = await sut.SendAsync(Message, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong. Please email owner@astardevelopment.co.uk directly.");
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_the_email_client_is_never_called()
    {
        var sut = CreateSut(emailClient, ConfiguredOptions());
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.SendAsync(Message, cancellationTokenSource.Token));

        await emailClient.DidNotReceive().SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
