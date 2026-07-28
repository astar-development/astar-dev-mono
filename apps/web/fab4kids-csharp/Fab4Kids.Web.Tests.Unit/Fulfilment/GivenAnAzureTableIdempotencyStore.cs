using AStar.Dev.FunctionalParadigm;
using Azure;
using Azure.Data.Tables;
using Fab4Kids.Web.Fulfilment;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Fulfilment;

public class GivenAnAzureTableIdempotencyStore
{
    private readonly TableClient tableClient = Substitute.For<TableClient>();
    private readonly ILogger<AzureTableIdempotencyStore> logger = Substitute.For<ILogger<AzureTableIdempotencyStore>>();

    private AzureTableIdempotencyStore CreateSut(TableClient? client) => new(logger, client);

    [Fact]
    public async Task when_the_table_client_is_not_configured_then_an_error_is_returned()
    {
        var sut = CreateSut(null);

        var result = await sut.TryMarkProcessedAsync("cs_test_123", TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong recording this order.");
    }

    [Fact]
    public async Task when_the_session_has_not_been_processed_then_it_is_marked_and_true_is_returned()
    {
        var sut = CreateSut(tableClient);

        var result = await sut.TryMarkProcessedAsync("cs_test_123", TestContext.Current.CancellationToken);

        result.Match(newlyMarked => newlyMarked, _ => false).ShouldBeTrue();
        await tableClient.Received(1).CreateIfNotExistsAsync(Arg.Any<CancellationToken>());
        await tableClient.Received(1).AddEntityAsync(
            Arg.Is<ProcessedSessionEntity>(e => e.PartitionKey == "processed-sessions" && e.RowKey == "cs_test_123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_session_has_already_been_processed_then_false_is_returned()
    {
        tableClient.AddEntityAsync(Arg.Any<ProcessedSessionEntity>(), Arg.Any<CancellationToken>())
            .Returns<Task<Response>>(_ => throw new RequestFailedException(409, "Conflict"));
        var sut = CreateSut(tableClient);

        var result = await sut.TryMarkProcessedAsync("cs_test_123", TestContext.Current.CancellationToken);

        result.Match(newlyMarked => newlyMarked, _ => true).ShouldBeFalse();
    }

    [Fact]
    public async Task when_marking_fails_unexpectedly_then_an_error_is_returned()
    {
        tableClient.AddEntityAsync(Arg.Any<ProcessedSessionEntity>(), Arg.Any<CancellationToken>())
            .Returns<Task<Response>>(_ => throw new RequestFailedException(500, "Table unavailable"));
        var sut = CreateSut(tableClient);

        var result = await sut.TryMarkProcessedAsync("cs_test_123", TestContext.Current.CancellationToken);

        result.Match(_ => "ok", err => err).ShouldBe("Something went wrong recording this order.");
    }
}
