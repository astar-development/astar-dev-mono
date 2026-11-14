using System.Net;
using System.Text.Json;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAnOneDriveItemRenamer : IDisposable
{
    private readonly OneDriveTransferFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static RemoteItemId Item => RemoteItemIdFactory.Create("item-1").Match(id => id, _ => throw new InvalidOperationException());

    private async Task<string> RenameAsync(string newName)
    {
        var result = await fixture.Renamer.RenameAsync(fixture.Account, Item, newName, CancellationToken);

        return result.Match(_ => "ok", error => error.GetType().Name);
    }

    [Fact]
    public async Task when_an_item_is_renamed_then_a_patch_carries_only_the_new_name()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.OK, "{}");

        var outcome = await RenameAsync("a-(2026-11-14T090507Z).txt");

        outcome.ShouldBe("ok");
        var patch = fixture.Graph.Requests.ShouldHaveSingleItem();
        patch.Method.ShouldBe(HttpMethod.Patch);
        patch.Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}item-1");
        patch.Authorization.ShouldBe($"Bearer {OneDriveTransferFixture.Token}");
        JsonDocument.Parse(patch.Body).RootElement.EnumerateObject().Select(property => (property.Name, property.Value.GetString())).ShouldBe([("name", "a-(2026-11-14T090507Z).txt")]);
    }

    [Fact]
    public async Task when_the_name_is_taken_then_the_conflict_is_a_name_conflict_and_nothing_is_replaced()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.Conflict, "{\"error\":{\"code\":\"nameAlreadyExists\"}}");

        (await RenameAsync("a.txt")).ShouldBe("NameConflict");
    }

    [Fact]
    public async Task when_the_item_is_gone_then_the_rename_reports_not_found()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.NotFound, "{}");

        (await RenameAsync("a.txt")).ShouldBe("NotFound");
    }

    [Theory]
    [InlineData(429, "Throttled")]
    [InlineData(401, "AuthenticationRequired")]
    [InlineData(403, "AccessDenied")]
    [InlineData(503, "Throttled")]
    [InlineData(500, "Transient")]
    public async Task when_the_provider_refuses_the_rename_then_the_status_is_mapped_to_a_provider_error(int status, string expected)
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json((HttpStatusCode)status, "{}");

        (await RenameAsync("a.txt")).ShouldBe(expected);
    }

    [Fact]
    public async Task when_there_is_no_access_token_then_the_rename_asks_for_a_sign_in_without_calling_the_provider()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Tokens.Outcome = AStarDev.CloudSync.Accounts.OneDrive.AccessTokenOutcomeFactory.CreateSignInRequired();

        (await RenameAsync("a.txt")).ShouldBe("AuthenticationRequired");
        fixture.Graph.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_rename_fails_then_the_logs_carry_no_item_name()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.Conflict, "{}");

        _ = await RenameAsync("secret-plan.txt");

        fixture.Logs.Logger.Messages.ShouldNotContain(message => message.Contains("secret-plan", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose() => fixture.Dispose();
}
