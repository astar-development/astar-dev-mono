using System.Globalization;
using AStar.Dev.FunctionalParadigm;
using Azure.Storage;
using Azure.Storage.Blobs;
using Fab4Kids.Web.Fulfilment;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Fulfilment;

public class GivenABlobSasDeliveryLinkGenerator
{
    private readonly ILogger<BlobSasDeliveryLinkGenerator> logger = Substitute.For<ILogger<BlobSasDeliveryLinkGenerator>>();

    private static BlobContainerClient FakeContainerClient() => new(
        new Uri("https://fakeaccount.blob.core.windows.net/pdfs"),
        new StorageSharedKeyCredential("fakeaccount", Convert.ToBase64String(new byte[32])));

    private BlobSasDeliveryLinkGenerator CreateSut(BlobContainerClient? client) => new(logger, client);

    [Fact]
    public async Task when_blob_storage_is_not_configured_then_an_error_is_returned()
    {
        var sut = CreateSut(null);

        var result = await sut.GenerateSignedUrlAsync("pdfs/file1.pdf", TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong generating your download link.");
    }

    [Fact]
    public async Task when_blob_storage_is_configured_then_a_signed_url_is_returned()
    {
        var sut = CreateSut(FakeContainerClient());

        var result = await sut.GenerateSignedUrlAsync("pdfs/file1.pdf", TestContext.Current.CancellationToken);

        string url = result.Match(value => value, _ => string.Empty);
        url.ShouldStartWith("https://fakeaccount.blob.core.windows.net/pdfs/pdfs/file1.pdf?");
        url.ShouldContain("sig=");
    }

    [Fact]
    public async Task when_blob_storage_is_configured_then_the_signed_url_expires_in_fifteen_minutes()
    {
        var sut = CreateSut(FakeContainerClient());
        var before = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(15));

        var result = await sut.GenerateSignedUrlAsync("pdfs/file1.pdf", TestContext.Current.CancellationToken);

        var after = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(15));
        string url = result.Match(value => value, _ => string.Empty);
        var expiresOn = DateTimeOffset.Parse(Uri.UnescapeDataString(new Uri(url).Query.Split("se=")[1].Split('&')[0]), CultureInfo.InvariantCulture);
        expiresOn.ShouldBeInRange(before.AddSeconds(-5), after.AddSeconds(5));
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_an_operation_cancelled_exception_is_thrown()
    {
        var sut = CreateSut(FakeContainerClient());
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GenerateSignedUrlAsync("pdfs/file1.pdf", cancellationTokenSource.Token));
    }
}
