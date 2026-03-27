using Microsoft.EntityFrameworkCore;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

public sealed class GivenAnAccountWithAMicrosoftIdentity
{
    [Fact]
    public async Task when_microsoft_account_id_changes_then_the_synthetic_guid_is_unchanged()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var account = new AccountBuilder().WithMicrosoftAccountId("original-ms-id").Build();
        context.Accounts.Add(account);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var originalGuid = account.Id;

        var loaded = await context.Accounts.FindAsync(new object?[] { originalGuid }, TestContext.Current.CancellationToken);
        loaded!.MicrosoftAccountId = "rotated-ms-id";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reloaded = await context.Accounts.FindAsync(new object?[] { originalGuid }, TestContext.Current.CancellationToken);
        reloaded.ShouldNotBeNull();
        reloaded.Id.ShouldBe(originalGuid);
        reloaded.MicrosoftAccountId.ShouldBe("rotated-ms-id");
    }

    [Fact]
    public async Task when_microsoft_account_id_changes_then_linked_child_rows_remain_intact()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        await context.CreateStubChildTableAsync("test_ms_id_child", cancellationToken: TestContext.Current.CancellationToken);

        var account = new AccountBuilder().WithMicrosoftAccountId("ms-original").Build();
        context.Accounts.Add(account);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO test_ms_id_child (id, account_id) VALUES ({0}, {1})",
            (IEnumerable<object>)[Guid.NewGuid().ToString(), account.Id.ToString("D").ToUpperInvariant()],
            TestContext.Current.CancellationToken);

        var loaded = await context.Accounts.FindAsync(new object?[] { account.Id }, TestContext.Current.CancellationToken);
        loaded!.MicrosoftAccountId = "ms-rotated";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        SqliteAssert.ChildRowCount(factory.Connection, "test_ms_id_child", account.Id, expected: 1);
    }

    [Fact]
    public async Task when_two_accounts_share_the_same_microsoft_account_id_then_each_has_a_distinct_guid()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var firstAccount  = new AccountBuilder().WithMicrosoftAccountId("shared-ms-id").Build();
        var secondAccount = new AccountBuilder().WithMicrosoftAccountId("shared-ms-id").Build();
        context.Accounts.AddRange(firstAccount, secondAccount);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var first  = await context.Accounts.FindAsync(new object?[] { firstAccount.Id  }, TestContext.Current.CancellationToken);
        var second = await context.Accounts.FindAsync(new object?[] { secondAccount.Id }, TestContext.Current.CancellationToken);
        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.Id.ShouldNotBe(second.Id);
    }
}
