using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;
using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenASyncRootChangedOverSqlite : MigratedDatabaseTests
{
    private const string FirstRoot = "/home/jbarden/CloudSync/OneDrive";
    private const string SecondRoot = "/home/jbarden/CloudSync/Elsewhere";
    private const string KeptRoot = "/home/jbarden/CloudSync/Other";

    private readonly AccountId changedAccount = NewAccount();
    private readonly AccountId keptAccount = NewAccount();

    private async Task<TermId> AddTermAsync(string text)
    {
        var stored = Ok(await Database.Categories.UpsertAsync(Category($"Category {text}"), CancellationToken));

        return Ok(await Database.Terms.UpsertAsync(Term(text, stored.Id), CancellationToken)).Id;
    }

    private async Task<IReadOnlyList<TermId>> TermsOfAsync(AccountId account, string path) => Ok(await Database.Links.GetTermsForFileAsync(account, Path(path), CancellationToken));

    private async Task<string> SetRootAsync(AccountId account, string path) =>
        (await Database.SyncRootWriter.SetAsync(account, Database.Folder(path), CancellationToken)).Match(root => root.Directory.FullName, rejection => rejection.GetType().Name);

    private async Task<(TermId Red, TermId Car)> LinkBothAccountsAsync()
    {
        _ = await SetRootAsync(changedAccount, FirstRoot);
        _ = await SetRootAsync(keptAccount, KeptRoot);
        var red = await AddTermAsync("red");
        var car = await AddTermAsync("car");
        foreach (var account in new[] { changedAccount, keptAccount })
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
    public async Task when_the_root_changes_then_the_accounts_auto_and_user_links_are_gone()
    {
        _ = await LinkBothAccountsAsync();

        var saved = await SetRootAsync(changedAccount, SecondRoot);

        (saved, (await TermsOfAsync(changedAccount, "auto.jpg")).Count, (await TermsOfAsync(changedAccount, "user.jpg")).Count).ShouldBe((SecondRoot, 0, 0));
    }

    [Fact]
    public async Task when_the_root_changes_then_the_other_account_keeps_its_links()
    {
        var (red, car) = await LinkBothAccountsAsync();

        _ = await SetRootAsync(changedAccount, SecondRoot);

        (await TermsOfAsync(keptAccount, "auto.jpg")).Concat(await TermsOfAsync(keptAccount, "user.jpg")).ShouldBe([red, car]);
    }

    [Fact]
    public async Task when_the_same_paths_are_classified_again_after_a_root_change_then_only_the_new_links_exist()
    {
        var (red, car) = await LinkBothAccountsAsync();
        _ = await SetRootAsync(changedAccount, SecondRoot);
        await InBaselineAsync(changedAccount, "auto.jpg", "user.jpg");

        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(changedAccount, Path("auto.jpg"), [car], CancellationToken));
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(changedAccount, Path("user.jpg"), [red], CancellationToken));

        (await TermsOfAsync(changedAccount, "auto.jpg")).Concat(await TermsOfAsync(changedAccount, "user.jpg")).ShouldBe([car, red]);
    }

    [Fact]
    public async Task when_the_same_root_is_saved_again_then_the_links_stay()
    {
        var (red, car) = await LinkBothAccountsAsync();

        _ = await SetRootAsync(changedAccount, FirstRoot + "/");

        (await TermsOfAsync(changedAccount, "auto.jpg")).Concat(await TermsOfAsync(changedAccount, "user.jpg")).ShouldBe([red, car]);
    }

    [Fact]
    public async Task when_the_links_cannot_be_removed_then_the_root_change_is_still_saved()
    {
        _ = await SetRootAsync(changedAccount, FirstRoot);
        await DropFileLinksAsync();

        var saved = await SetRootAsync(changedAccount, SecondRoot);

        saved.ShouldBe(SecondRoot);
    }
}
