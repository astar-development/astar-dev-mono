using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;
using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenAnAccountRemovedOverSqlite : MigratedDatabaseTests
{
    private readonly AccountId removedAccount = NewAccount();
    private readonly AccountId keptAccount = NewAccount();

    private async Task<TermId> AddTermAsync(string text)
    {
        var stored = Ok(await Database.Categories.UpsertAsync(Category($"Category {text}"), CancellationToken));

        return Ok(await Database.Terms.UpsertAsync(Term(text, stored.Id), CancellationToken)).Id;
    }

    private async Task<IReadOnlyList<TermId>> TermsOfAsync(AccountId account, string path) => Ok(await Database.Links.GetTermsForFileAsync(account, Path(path), CancellationToken));

    private async Task<(TermId Red, TermId Car)> LinkBothAccountsAsync()
    {
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        foreach (var account in new[] { removedAccount, keptAccount })
        {
            await InBaselineAsync(account, "auto.jpg", "user.jpg");
            _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("auto.jpg"), [red], CancellationToken));
            _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("user.jpg"), car, CancellationToken));
        }

        return (red, car);
    }

    private async Task DropFileLinksAsync()
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database.DatabasePath, Pooling = false }.ToString());
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DROP TABLE FileLinks;";
        _ = await command.ExecuteNonQueryAsync(CancellationToken);
    }

    [Fact]
    public async Task when_the_account_is_removed_then_its_auto_and_user_links_are_gone()
    {
        _ = await LinkBothAccountsAsync();

        var removed = await Database.LinksParticipant.RemoveAccountDataAsync(removedAccount, CancellationToken);

        (removed.Match(_ => true, _ => false), (await TermsOfAsync(removedAccount, "auto.jpg")).Count, (await TermsOfAsync(removedAccount, "user.jpg")).Count).ShouldBe((true, 0, 0));
    }

    [Fact]
    public async Task when_the_account_is_removed_then_the_other_account_keeps_its_links()
    {
        var (red, car) = await LinkBothAccountsAsync();

        _ = await Database.LinksParticipant.RemoveAccountDataAsync(removedAccount, CancellationToken);

        (await TermsOfAsync(keptAccount, "auto.jpg")).Concat(await TermsOfAsync(keptAccount, "user.jpg")).ShouldBe([red, car]);
    }

    [Fact]
    public async Task when_the_account_is_removed_then_the_terms_and_categories_stay()
    {
        _ = await LinkBothAccountsAsync();

        _ = await Database.LinksParticipant.RemoveAccountDataAsync(removedAccount, CancellationToken);

        (Ok(await Database.Terms.GetAllAsync(CancellationToken)).Count, Ok(await Database.Categories.GetAllAsync(CancellationToken)).Count).ShouldBe((2, 2));
    }

    [Fact]
    public async Task when_an_account_with_no_links_is_removed_then_it_succeeds()
    {
        var removed = await Database.LinksParticipant.RemoveAccountDataAsync(removedAccount, CancellationToken);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_links_cannot_be_deleted_then_the_removal_fails()
    {
        await DropFileLinksAsync();

        var removed = await Database.LinksParticipant.RemoveAccountDataAsync(removedAccount, CancellationToken);

        removed.Match(_ => "removed", error => error.Message).ShouldBe("The account's classification links could not be removed.");
    }

    [Fact]
    public async Task when_the_removal_is_cancelled_then_it_throws_and_the_links_stay()
    {
        var (red, _) = await LinkBothAccountsAsync();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => Database.LinksParticipant.RemoveAccountDataAsync(removedAccount, cancelled.Token));

        (await TermsOfAsync(removedAccount, "auto.jpg")).ShouldBe([red]);
    }
}
