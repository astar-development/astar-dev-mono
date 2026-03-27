using Microsoft.EntityFrameworkCore;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

public sealed class GivenAnAccountWithLinkedRows
{
    [Fact]
    public async Task when_the_account_is_deleted_then_all_linked_rows_are_removed()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        await context.CreateStubChildTableAsync("test_account_child", cancellationToken: TestContext.Current.CancellationToken);

        var account = new AccountBuilder().Build();
        context.Accounts.Add(account);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO test_account_child (id, account_id) VALUES ({0}, {1})",
            (IEnumerable<object>)[Guid.NewGuid().ToString(), account.Id.ToString("D").ToUpperInvariant()],
            TestContext.Current.CancellationToken);
        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", account.Id, expected: 1);

        context.Accounts.Remove(account);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", account.Id, expected: 0);
    }

    [Fact]
    public async Task when_one_of_many_accounts_is_deleted_then_only_its_linked_rows_are_removed()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        await context.CreateStubChildTableAsync("test_account_child", cancellationToken: TestContext.Current.CancellationToken);

        var keptAccount    = new AccountBuilder().Build();
        var deletedAccount = new AccountBuilder().Build();
        context.Accounts.AddRange(keptAccount, deletedAccount);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO test_account_child (id, account_id) VALUES ({0}, {1})",
            (IEnumerable<object>)[Guid.NewGuid().ToString(), keptAccount.Id.ToString("D").ToUpperInvariant()],
            TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO test_account_child (id, account_id) VALUES ({0}, {1})",
            (IEnumerable<object>)[Guid.NewGuid().ToString(), deletedAccount.Id.ToString("D").ToUpperInvariant()],
            TestContext.Current.CancellationToken);

        context.Accounts.Remove(deletedAccount);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", deletedAccount.Id, expected: 0);
        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", keptAccount.Id,    expected: 1);
    }
}
