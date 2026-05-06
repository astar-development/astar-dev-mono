using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Repositories;

public sealed class GivenAnAccountRepository : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _seedingContext;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public GivenAnAccountRepository()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _seedingContext = new AppDbContext(options);
        _ = _seedingContext.Database.EnsureCreated();

        _factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(new AppDbContext(options)));
    }

    public void Dispose()
    {
        _seedingContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task when_getting_all_with_no_accounts_then_empty_list_is_returned()
    {
        var repository = new AccountRepository(_factory);

        var result = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_getting_all_with_single_account_then_account_is_returned()
    {
        var repository = new AccountRepository(_factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create("User", "user@outlook.com") };
        _ = _seedingContext.Accounts.Add(account);
        _ = await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(new AccountId("user-1"));
    }

    [Fact]
    public async Task when_getting_all_then_accounts_are_ordered_by_email()
    {
        var repository = new AccountRepository(_factory);
        _seedingContext.Accounts.AddRange(
            new AccountEntity { Id = new AccountId("user-3"), Profile = AccountProfileFactory.Create("Z User", "zebra@outlook.com") },
            new AccountEntity { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create("A User", "alice@outlook.com") },
            new AccountEntity { Id = new AccountId("user-2"), Profile = AccountProfileFactory.Create("B User", "bob@outlook.com") }
        );
        _ = await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(3);
        result[0].Profile.Email.ShouldBe("alice@outlook.com");
        result[1].Profile.Email.ShouldBe("bob@outlook.com");
        result[2].Profile.Email.ShouldBe("zebra@outlook.com");
    }

    [Fact]
    public async Task when_getting_by_id_with_existing_id_then_some_is_returned()
    {
        var repository = new AccountRepository(_factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create("User", "user@outlook.com") };
        _ = _seedingContext.Accounts.Add(account);
        _ = await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetByIdAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        result.IsSome().ShouldBeTrue();
        result.Match(entity => entity.Profile.Email, () => string.Empty).ShouldBe("user@outlook.com");
    }

    [Fact]
    public async Task when_getting_by_id_with_non_existing_id_then_none_is_returned()
    {
        var repository = new AccountRepository(_factory);

        var result = await repository.GetByIdAsync(new AccountId("non-existent"), TestContext.Current.CancellationToken);

        result.IsNone().ShouldBeTrue();
    }

    [Fact]
    public async Task when_upserting_new_account_then_account_is_inserted()
    {
        var repository = new AccountRepository(_factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create("User", "user@outlook.com") };

        await repository.UpsertAsync(account, TestContext.Current.CancellationToken);

        var retrieved = await _seedingContext.Accounts.FindAsync([new AccountId("user-1")], cancellationToken: TestContext.Current.CancellationToken);
        _ = retrieved.ShouldNotBeNull();
        retrieved.Profile.Email.ShouldBe("user@outlook.com");
    }

    [Fact]
    public async Task when_upserting_existing_account_then_account_is_updated()
    {
        var repository = new AccountRepository(_factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create("User", "user@outlook.com") };
        _ = _seedingContext.Accounts.Add(account);
        _ = await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        account.Profile = account.Profile with { DisplayName = "Updated User" };
        await repository.UpsertAsync(account, TestContext.Current.CancellationToken);

        await _seedingContext.Entry(account).ReloadAsync(TestContext.Current.CancellationToken);
        account.Profile.DisplayName.ShouldBe("Updated User");
    }

    [Fact]
    public async Task when_deleting_account_then_account_is_removed()
    {
        var repository = new AccountRepository(_factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create("User", "user@outlook.com") };
        _ = _seedingContext.Accounts.Add(account);
        _ = await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.DeleteAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        _seedingContext.ChangeTracker.Clear();
        var retrieved = await _seedingContext.Accounts.FindAsync([new AccountId("user-1")], cancellationToken: TestContext.Current.CancellationToken);
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task when_setting_active_account_then_one_account_is_active()
    {
        var repository = new AccountRepository(_factory);
        _seedingContext.Accounts.AddRange(
            new AccountEntity { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create("User 1", "user1@outlook.com"), IsActive = true },
            new AccountEntity { Id = new AccountId("user-2"), Profile = AccountProfileFactory.Create("User 2", "user2@outlook.com"), IsActive = false }
        );
        _ = await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.SetActiveAccountAsync(new AccountId("user-2"), TestContext.Current.CancellationToken);

        await _seedingContext.Entry(_seedingContext.Accounts.Local.First(a => a.Id == new AccountId("user-1"))).ReloadAsync(TestContext.Current.CancellationToken);
        await _seedingContext.Entry(_seedingContext.Accounts.Local.First(a => a.Id == new AccountId("user-2"))).ReloadAsync(TestContext.Current.CancellationToken);
        _seedingContext.Accounts.Local.First(a => a.Id == new AccountId("user-1")).IsActive.ShouldBeFalse();
        _seedingContext.Accounts.Local.First(a => a.Id == new AccountId("user-2")).IsActive.ShouldBeTrue();
    }
}
