using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.DeltaQueries;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.DeltaQueries;

public sealed class GivenADeltaQueryService
{
    private readonly IDeltaQueryService _sut = Substitute.For<IDeltaQueryService>();

    [Fact]
    public async Task when_get_delta_is_called_with_null_token_then_returns_full_sync_result()
    {
        var items  = (IReadOnlyList<DeltaItem>)[DeltaItemFactory.Create("id1", "Documents", null, DeltaItemType.Folder)];
        var result = new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create(items, "https://delta-link", isFullSync: true));
        _sut.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Is<string?>(x => x == null), Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await _sut.GetDeltaAsync("token", "folder-id", null, TestContext.Current.CancellationToken);

        var ok = response.ShouldBeOfType<Result<DeltaQueryResult, DeltaQueryError>.Ok>();
        ok.Value.IsFullSync.ShouldBeTrue();
        ok.Value.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_get_delta_is_called_with_existing_token_then_returns_incremental_result()
    {
        var items  = (IReadOnlyList<DeltaItem>)[DeltaItemFactory.Create("id2", "report.docx", "folder1", DeltaItemType.File)];
        var result = new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create(items, "https://next-delta-link", isFullSync: false));
        _sut.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await _sut.GetDeltaAsync("token", "folder-id", "https://previous-delta-link", TestContext.Current.CancellationToken);

        var ok = response.ShouldBeOfType<Result<DeltaQueryResult, DeltaQueryError>.Ok>();
        ok.Value.IsFullSync.ShouldBeFalse();
    }

    [Fact]
    public async Task when_delta_token_is_expired_then_returns_token_expired_error()
    {
        _sut.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Error(DeltaQueryErrorFactory.TokenExpired()));

        var response = await _sut.GetDeltaAsync("token", "folder-id", "stale-token", TestContext.Current.CancellationToken);

        response.ShouldBeOfType<Result<DeltaQueryResult, DeltaQueryError>.Error>()
                .Reason.ShouldBeOfType<DeltaTokenExpiredError>();
    }

    [Fact]
    public async Task when_graph_api_throttles_after_max_retries_then_returns_throttled_error()
    {
        _sut.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Error(DeltaQueryErrorFactory.Throttled("Max retries exceeded after throttling")));

        var response = await _sut.GetDeltaAsync("token", "folder-id", null, TestContext.Current.CancellationToken);

        response.ShouldBeOfType<Result<DeltaQueryResult, DeltaQueryError>.Error>()
                .Reason.ShouldBeOfType<DeltaQueryThrottledError>();
    }

    [Fact]
    public async Task when_get_delta_returns_folder_renamed_item_then_item_type_is_folder_renamed()
    {
        var renamedItem = DeltaItemFactory.Create("folder1", "NewName", "parent1", DeltaItemType.FolderRenamed, "OldName");
        var items       = (IReadOnlyList<DeltaItem>)[renamedItem];
        _sut.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create(items, "https://delta-link", isFullSync: false)));

        var response = await _sut.GetDeltaAsync("token", "parent1", "token-value", TestContext.Current.CancellationToken);

        var ok = response.ShouldBeOfType<Result<DeltaQueryResult, DeltaQueryError>.Ok>();
        ok.Value.Items.ShouldHaveSingleItem().ItemType.ShouldBe(DeltaItemType.FolderRenamed);
    }

    [Fact]
    public async Task when_get_delta_returns_deleted_item_then_item_type_is_deleted()
    {
        var deletedItem = DeltaItemFactory.CreateDeleted("deleted-id");
        var items       = (IReadOnlyList<DeltaItem>)[deletedItem];
        _sut.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create(items, "https://delta-link", isFullSync: false)));

        var response = await _sut.GetDeltaAsync("token", "folder-id", "token-value", TestContext.Current.CancellationToken);

        var ok = response.ShouldBeOfType<Result<DeltaQueryResult, DeltaQueryError>.Ok>();
        ok.Value.Items.ShouldHaveSingleItem().ItemType.ShouldBe(DeltaItemType.Deleted);
    }

    [Fact]
    public async Task when_get_delta_returns_empty_list_then_items_is_empty()
    {
        var result = new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create([], "https://delta-link", isFullSync: false));
        _sut.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await _sut.GetDeltaAsync("token", "folder-id", "token-value", TestContext.Current.CancellationToken);

        response.ShouldBeOfType<Result<DeltaQueryResult, DeltaQueryError>.Ok>().Value.Items.ShouldBeEmpty();
    }
}
