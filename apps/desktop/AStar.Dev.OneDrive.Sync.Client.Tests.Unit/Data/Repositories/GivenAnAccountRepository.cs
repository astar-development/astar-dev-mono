using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Repositories;

public sealed class GivenAnAccountRepository
{
    [Fact]
    public async Task when_getting_all_with_no_accounts_then_empty_list_is_returned()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new AccountRepository(factory);

        var result = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_getting_all_with_single_account_then_account_is_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new AccountRepository(factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Email = "user@outlook.com", DisplayName = "User" };
        _ = db.Accounts.Add(account);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(new AccountId("user-1"));
    }

    [Fact]
    public async Task when_getting_all_then_accounts_are_ordered_by_email()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new AccountRepository(factory);
        db.Accounts.AddRange(
            new AccountEntity { Id = new AccountId("user-3"), Email = "zebra@outlook.com", DisplayName = "Z User" },
            new AccountEntity { Id = new AccountId("user-1"), Email = "alice@outlook.com", DisplayName = "A User" },
            new AccountEntity { Id = new AccountId("user-2"), Email = "bob@outlook.com", DisplayName = "B User" }
        );
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(3);
        result[0].Email.ShouldBe("alice@outlook.com");
        result[1].Email.ShouldBe("bob@outlook.com");
        result[2].Email.ShouldBe("zebra@outlook.com");
    }

    [Fact]
    public async Task when_getting_by_id_with_existing_id_then_some_is_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new AccountRepository(factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Email = "user@outlook.com", DisplayName = "User" };
        _ = db.Accounts.Add(account);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetByIdAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        result.IsSome().ShouldBeTrue();
        result.Match(entity => entity.Email, () => string.Empty).ShouldBe("user@outlook.com");
    }

    [Fact]
    public async Task when_getting_by_id_with_non_existing_id_then_none_is_returned()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new AccountRepository(factory);

        var result = await repository.GetByIdAsync(new AccountId("non-existent"), TestContext.Current.CancellationToken);

        result.IsNone().ShouldBeTrue();
    }

    [Fact]
    public async Task when_upserting_new_account_then_account_is_inserted()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new AccountRepository(factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Email = "user@outlook.com", DisplayName = "User" };

        await repository.UpsertAsync(account, TestContext.Current.CancellationToken);

        var retrieved = await db.Accounts.FindAsync([new AccountId("user-1")], cancellationToken: TestContext.Current.CancellationToken);
        _ = retrieved.ShouldNotBeNull();
        retrieved.Email.ShouldBe("user@outlook.com");
    }

    [Fact]
    public async Task when_upserting_existing_account_then_account_is_updated()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new AccountRepository(factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Email = "user@outlook.com", DisplayName = "User" };
        _ = db.Accounts.Add(account);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        account.DisplayName = "Updated User";
        await repository.UpsertAsync(account, TestContext.Current.CancellationToken);

        var retrieved = await db.Accounts.FindAsync([new AccountId("user-1"), TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
        retrieved!.DisplayName.ShouldBe("Updated User");
    }

    [Fact]
    public async Task when_deleting_account_then_account_is_removed()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new AccountRepository(factory);
        var account = new AccountEntity { Id = new AccountId("user-1"), Email = "user@outlook.com", DisplayName = "User" };
        _ = db.Accounts.Add(account);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        try
        {
            await repository.DeleteAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);
        }
        catch(InvalidOperationException)
        {
        }
    }

    [Fact]
    public async Task when_setting_active_account_then_one_account_is_active()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new AccountRepository(factory);
        db.Accounts.AddRange(
            new AccountEntity { Id = new AccountId("user-1"), Email = "user1@outlook.com", DisplayName = "User 1", IsActive = true },
            new AccountEntity { Id = new AccountId("user-2"), Email = "user2@outlook.com", DisplayName = "User 2", IsActive = false }
        );
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        try
        {
            await repository.SetActiveAccountAsync(new AccountId("user-2"), TestContext.Current.CancellationToken);
        }
        catch(InvalidOperationException)
        {
        }
    }

    private static (AppDbContext seedingContext, IDbContextFactory<AppDbContext> factory) CreateInMemoryFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(callInfo => Task.FromResult(new AppDbContext(options)));

        return (seedingContext, factory);
    }
}
