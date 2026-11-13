using System.Net;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAnOneDriveItemRemover : IDisposable
{
    private readonly OneDriveTransferFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static RemoteItemId Item => RemoteItemIdFactory.Create("item-1").Match(id => id, _ => throw new InvalidOperationException());

    private static RemovePrecondition ByContent => RemovePreconditionFactory.CreateMustMatchContent(new ContentTag("ctag-3")).Match(found => found, _ => throw new InvalidOperationException());

    private static RemovePrecondition ByEntity => RemovePreconditionFactory.CreateMustMatch(new EntityTag("etag-9")).Match(found => found, _ => throw new InvalidOperationException());

    private async Task<string> RemoveAsync(RemovePrecondition precondition)
    {
        var result = await fixture.Remover.RemoveAsync(fixture.Account, Item, precondition, CancellationToken);

        return result.Match(_ => "ok", error => error.GetType().Name);
    }

    [Fact]
    public async Task when_a_file_is_removed_by_content_tag_then_the_delete_carries_that_tag_in_if_match()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        var outcome = await RemoveAsync(ByContent);

        outcome.ShouldBe("ok");
        var delete = fixture.Graph.Requests.ShouldHaveSingleItem();
        delete.Method.ShouldBe(HttpMethod.Delete);
        delete.Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}item-1");
        delete.Headers["If-Match"].ShouldBe("ctag-3");
        delete.Authorization.ShouldBe($"Bearer {OneDriveTransferFixture.Token}");
    }

    [Fact]
    public async Task when_a_folder_is_removed_by_entity_tag_then_the_delete_carries_that_tag_in_if_match()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        _ = await RemoveAsync(ByEntity);

        fixture.Graph.Requests.ShouldHaveSingleItem().Headers["If-Match"].ShouldBe("etag-9");
    }

    [Fact]
    public async Task when_the_item_is_already_gone_then_the_removal_counts_as_success()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.NotFound, "{}");

        (await RemoveAsync(ByContent)).ShouldBe("ok");
    }

    [Theory]
    [InlineData(412, "PreconditionFailed")]
    [InlineData(429, "Throttled")]
    [InlineData(401, "AuthenticationRequired")]
    [InlineData(403, "AccessDenied")]
    [InlineData(503, "Throttled")]
    [InlineData(500, "Transient")]
    public async Task when_the_provider_refuses_the_delete_then_the_status_is_mapped_to_a_provider_error(int status, string expected)
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json((HttpStatusCode)status, "{}");

        (await RemoveAsync(ByContent)).ShouldBe(expected);
    }

    [Fact]
    public async Task when_there_is_no_access_token_then_the_removal_asks_for_a_sign_in_without_calling_the_provider()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Tokens.Outcome = AStarDev.CloudSync.Accounts.OneDrive.AccessTokenOutcomeFactory.CreateSignInRequired();

        (await RemoveAsync(ByContent)).ShouldBe("AuthenticationRequired");
        fixture.Graph.Requests.ShouldBeEmpty();
    }

    public void Dispose() => fixture.Dispose();
}
