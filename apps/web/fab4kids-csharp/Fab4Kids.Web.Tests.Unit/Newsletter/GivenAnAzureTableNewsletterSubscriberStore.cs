using AStar.Dev.FunctionalParadigm;
using Azure;
using Azure.Data.Tables;
using Fab4Kids.Web.Newsletter;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Newsletter;

public class GivenAnAzureTableNewsletterSubscriberStore
{
    private readonly TableClient tableClient = Substitute.For<TableClient>();
    private readonly ILogger<AzureTableNewsletterSubscriberStore> logger = Substitute.For<ILogger<AzureTableNewsletterSubscriberStore>>();

    private AzureTableNewsletterSubscriberStore CreateSut(TableClient? client) => new(logger, client);

    [Fact]
    public async Task when_the_table_client_is_not_configured_then_exists_returns_an_error()
    {
        var sut = CreateSut(null);

        var result = await sut.ExistsAsync("ada@example.com", TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong. Please try again later.");
    }

    [Fact]
    public async Task when_the_table_client_is_not_configured_then_add_returns_an_error()
    {
        var sut = CreateSut(null);
        var subscriber = NewsletterSubscriberFactory.Create("ada@example.com", DateTimeOffset.UtcNow);

        var result = await sut.AddAsync(subscriber, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong. Please try again later.");
    }

    [Fact]
    public async Task when_the_subscriber_is_not_found_then_exists_returns_false()
    {
        tableClient.GetEntityAsync<NewsletterSubscriberEntity>("subscribers", "ada@example.com", null, Arg.Any<CancellationToken>())
            .Returns<Task<Response<NewsletterSubscriberEntity>>>(_ => throw new RequestFailedException(404, "Not Found"));
        var sut = CreateSut(tableClient);

        var result = await sut.ExistsAsync("ada@example.com", TestContext.Current.CancellationToken);

        result.Match(exists => exists, _ => false).ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_subscriber_is_found_then_exists_returns_true()
    {
        var entity = new NewsletterSubscriberEntity { PartitionKey = "subscribers", RowKey = "ada@example.com" };
        tableClient.GetEntityAsync<NewsletterSubscriberEntity>("subscribers", "ada@example.com", null, Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(entity, Substitute.For<Response>()));
        var sut = CreateSut(tableClient);

        var result = await sut.ExistsAsync("ada@example.com", TestContext.Current.CancellationToken);

        result.Match(exists => exists, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_adding_a_subscriber_then_the_table_is_created_if_needed_and_the_entity_is_upserted()
    {
        var sut = CreateSut(tableClient);
        var subscriber = NewsletterSubscriberFactory.Create("Ada@Example.com", DateTimeOffset.UtcNow);

        var result = await sut.AddAsync(subscriber, TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        await tableClient.Received(1).CreateIfNotExistsAsync(Arg.Any<CancellationToken>());
        await tableClient.Received(1).UpsertEntityAsync(
            Arg.Is<NewsletterSubscriberEntity>(e => e.PartitionKey == "subscribers" && e.RowKey == "ada@example.com"),
            TableUpdateMode.Replace,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_upserting_throws_then_add_returns_an_error()
    {
        tableClient.UpsertEntityAsync(Arg.Any<NewsletterSubscriberEntity>(), Arg.Any<TableUpdateMode>(), Arg.Any<CancellationToken>())
            .Returns<Task<Response>>(_ => throw new RequestFailedException(500, "Table unavailable"));
        var sut = CreateSut(tableClient);
        var subscriber = NewsletterSubscriberFactory.Create("ada@example.com", DateTimeOffset.UtcNow);

        var result = await sut.AddAsync(subscriber, TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong saving your subscription.");
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_exists_never_calls_the_table_client()
    {
        var sut = CreateSut(tableClient);
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.ExistsAsync("ada@example.com", cancellationTokenSource.Token));

        await tableClient.DidNotReceive().GetEntityAsync<NewsletterSubscriberEntity>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_add_never_calls_the_table_client()
    {
        var sut = CreateSut(tableClient);
        var subscriber = NewsletterSubscriberFactory.Create("ada@example.com", DateTimeOffset.UtcNow);
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.AddAsync(subscriber, cancellationTokenSource.Token));

        await tableClient.DidNotReceive().CreateIfNotExistsAsync(Arg.Any<CancellationToken>());
    }
}
