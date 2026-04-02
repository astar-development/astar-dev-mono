using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Features.Accounts;

public sealed class GivenAnAccountRepository
{
    [Fact]
    public async Task when_account_is_added_then_it_can_be_retrieved()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        var sut = new AccountRepository(context);

        var account = new AccountBuilder().Build();
        await sut.AddAsync(account, TestContext.Current.CancellationToken);

        var loaded = await sut.FindByIdAsync(account.Id, TestContext.Current.CancellationToken);
        _ = loaded.ShouldNotBeNull();
        loaded.DisplayName.ShouldBe(account.DisplayName);
    }

    [Fact]
    public async Task when_account_is_removed_then_it_is_no_longer_retrievable()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        var sut = new AccountRepository(context);

        var account = new AccountBuilder().Build();
        await sut.AddAsync(account, TestContext.Current.CancellationToken);
        await sut.RemoveAsync(account.Id, TestContext.Current.CancellationToken);

        var loaded = await sut.FindByIdAsync(account.Id, TestContext.Current.CancellationToken);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task when_account_is_removed_then_synced_file_metadata_rows_are_cascade_deleted()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        var sut = new AccountRepository(context);

        var account = new AccountBuilder().Build();
        await sut.AddAsync(account, TestContext.Current.CancellationToken);

        context.SyncedFileMetadata.Add(new SyncedFileMetadata
        {
            AccountId       = account.Id,
            RemoteItemId    = "remote-1",
            RelativePath    = "docs/file.txt",
            FileName        = "file.txt",
            FileSizeBytes   = 1024,
            Sha256Checksum  = new string('a', 64),
            LastModifiedUtc = DateTimeOffset.UtcNow,
            CreatedUtc      = DateTimeOffset.UtcNow,
        });
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await sut.RemoveAsync(account.Id, TestContext.Current.CancellationToken);

        var remaining = await context.SyncedFileMetadata
            .CountAsync(meta => meta.AccountId == account.Id, TestContext.Current.CancellationToken);
        remaining.ShouldBe(0);
    }

    [Fact]
    public async Task when_account_is_updated_then_changes_are_persisted()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        var sut = new AccountRepository(context);

        var account = new AccountBuilder().Build();
        await sut.AddAsync(account, TestContext.Current.CancellationToken);

        account.LocalSyncPath = "/home/user/OneDrive/Alice";
        account.SyncIntervalMinutes = 30;
        await sut.UpdateAsync(account, TestContext.Current.CancellationToken);

        var loaded = await sut.FindByIdAsync(account.Id, TestContext.Current.CancellationToken);
        _ = loaded.ShouldNotBeNull();
        loaded.LocalSyncPath.ShouldBe("/home/user/OneDrive/Alice");
        loaded.SyncIntervalMinutes.ShouldBe(30);
    }

    [Fact]
    public async Task when_get_all_sync_paths_then_only_accounts_with_paths_are_returned()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        var sut = new AccountRepository(context);

        var withPath    = new AccountBuilder().Build();
        var withoutPath = new AccountBuilder().Build();
        withPath.LocalSyncPath = "/home/user/OneDrive/Alice";

        await sut.AddAsync(withPath, TestContext.Current.CancellationToken);
        await sut.AddAsync(withoutPath, TestContext.Current.CancellationToken);

        var paths = await sut.GetAllSyncPathsAsync(TestContext.Current.CancellationToken);

        paths.ShouldHaveSingleItem();
        paths[0].LocalSyncPath.ShouldBe("/home/user/OneDrive/Alice");
    }
}
