using Microsoft.EntityFrameworkCore;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Features.Accounts;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

public sealed class GivenAnAccountWithLinkedRows
{
    [Fact]
    public async Task when_the_account_is_deleted_then_all_linked_rows_are_removed()
    {
        await using var factory = AppDbContextFactory.Create();
        await using AppDbContext context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        await context.CreateStubChildTableAsync("test_account_child", cancellationToken: TestContext.Current.CancellationToken);

        Account account = new AccountBuilder().Build();
        _ = context.Accounts.Add(account);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ = await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO test_account_child (id, account_id) VALUES ({0}, {1})",
            (IEnumerable<object>)[Guid.NewGuid().ToString(), account.Id.ToString("D").ToUpperInvariant()],
            TestContext.Current.CancellationToken);
        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", account.Id, expected: 1);

        _ = context.Accounts.Remove(account);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", account.Id, expected: 0);
    }

    [Fact]
    public async Task when_one_of_many_accounts_is_deleted_then_only_its_linked_rows_are_removed()
    {
        await using var factory = AppDbContextFactory.Create();
        await using AppDbContext context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        await context.CreateStubChildTableAsync("test_account_child", cancellationToken: TestContext.Current.CancellationToken);

        Account keptAccount    = new AccountBuilder().Build();
        Account deletedAccount = new AccountBuilder().Build();
        context.Accounts.AddRange(keptAccount, deletedAccount);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ = await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO test_account_child (id, account_id) VALUES ({0}, {1})",
            (IEnumerable<object>)[Guid.NewGuid().ToString(), keptAccount.Id.ToString("D").ToUpperInvariant()],
            TestContext.Current.CancellationToken);
        _ = await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO test_account_child (id, account_id) VALUES ({0}, {1})",
            (IEnumerable<object>)[Guid.NewGuid().ToString(), deletedAccount.Id.ToString("D").ToUpperInvariant()],
            TestContext.Current.CancellationToken);

        _ = context.Accounts.Remove(deletedAccount);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", deletedAccount.Id, expected: 0);
        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", keptAccount.Id,    expected: 1);
    }
}
