using AStar.Dev.FunctionalParadigm;
using Azure;
using Azure.Communication.Email;
using Fab4Kids.Web.Fulfilment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Fulfilment;

public class GivenAnAzureDeliveryEmailSender
{
    private readonly EmailClient emailClient = Substitute.For<EmailClient>();
    private readonly ILogger<AzureDeliveryEmailSender> logger = Substitute.For<ILogger<AzureDeliveryEmailSender>>();

    private static readonly IReadOnlyList<DeliveryLink> Links = [DeliveryLinkFactory.Create("Times Tables Pack", "https://example.blob.core.windows.net/pdfs/file1.pdf?sig=abc", DateTimeOffset.UtcNow.AddMinutes(15))];

    private AzureDeliveryEmailSender CreateSut(EmailClient? client, FulfilmentOptions options) => new(Options.Create(options), logger, client);

    private static FulfilmentOptions ConfiguredOptions() => new() { EmailConnectionString = "endpoint=https://acs.example.com/;accesskey=key", FromAddress = "Orders@fab-4-kids.co.uk" };

    [Fact]
    public async Task when_email_is_not_configured_then_an_error_is_returned()
    {
        var sut = CreateSut(null, ConfiguredOptions());

        var result = await sut.SendAsync("ada@example.com", "cs_test_123", Links, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong sending your download links.");
    }

    [Fact]
    public async Task when_the_from_address_is_missing_then_an_error_is_returned()
    {
        var sut = CreateSut(emailClient, new FulfilmentOptions { EmailConnectionString = "endpoint=https://acs.example.com/;accesskey=key" });

        var result = await sut.SendAsync("ada@example.com", "cs_test_123", Links, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong sending your download links.");
    }

    [Fact]
    public async Task when_configured_then_the_email_is_sent_to_the_customer()
    {
        var sut = CreateSut(emailClient, ConfiguredOptions());

        var result = await sut.SendAsync("ada@example.com", "cs_test_123", Links, TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        await emailClient.Received(1).SendAsync(WaitUntil.Completed, Arg.Is<EmailMessage>(message => message.SenderAddress == "Orders@fab-4-kids.co.uk"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_sending_throws_then_an_error_is_returned()
    {
        emailClient.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task<EmailSendOperation>>(_ => throw new RequestFailedException(500, "ACS unavailable"));
        var sut = CreateSut(emailClient, ConfiguredOptions());

        var result = await sut.SendAsync("ada@example.com", "cs_test_123", Links, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong sending your download links.");
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_the_email_client_is_never_called()
    {
        var sut = CreateSut(emailClient, ConfiguredOptions());
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.SendAsync("ada@example.com", "cs_test_123", Links, cancellationTokenSource.Token));

        await emailClient.DidNotReceive().SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
