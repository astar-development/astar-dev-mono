using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Data;

public sealed class GivenSyncedItemRepository_WhenSearchingForDuplicates(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task when_no_duplicate_files_exist_then_returns_empty_list()
    {
        var ct = TestContext.Current.CancellationToken;
        var accountId = new AccountId("dup-search-no-dups");
        await SeedAccountAsync(accountId, ct);
        await SeedFileAsync(accountId, "/folder/alpha.pdf", 100L, ct);
        await SeedFileAsync(accountId, "/folder/beta.pdf",  200L, ct);

        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var criteria = SyncedItemSearchCriteriaFactory.Create(accountId, duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, ct);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_files_share_size_and_filename_then_both_are_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var accountId = new AccountId("dup-search-has-dups");
        await SeedAccountAsync(accountId, ct);
        await SeedFileAsync(accountId, "/dir1/report.pdf", 512L, ct);
        await SeedFileAsync(accountId, "/dir2/report.pdf", 512L, ct);

        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var criteria = SyncedItemSearchCriteriaFactory.Create(accountId, duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, ct);

        results.Count.ShouldBe(2);
        results.ShouldAllBe(r => r.RemotePath.EndsWith("report.pdf", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_files_have_same_size_but_different_names_then_not_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var accountId = new AccountId("dup-search-same-size-diff-name");
        await SeedAccountAsync(accountId, ct);
        await SeedFileAsync(accountId, "/folder/alpha.jpg", 1024L, ct);
        await SeedFileAsync(accountId, "/folder/beta.jpg",  1024L, ct);

        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var criteria = SyncedItemSearchCriteriaFactory.Create(accountId, duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, ct);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_files_have_same_name_but_different_sizes_then_not_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var accountId = new AccountId("dup-search-same-name-diff-size");
        await SeedAccountAsync(accountId, ct);
        await SeedFileAsync(accountId, "/dir1/document.docx", 100L, ct);
        await SeedFileAsync(accountId, "/dir2/document.docx", 200L, ct);

        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var criteria = SyncedItemSearchCriteriaFactory.Create(accountId, duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, ct);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_three_files_share_size_and_name_then_all_three_are_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var accountId = new AccountId("dup-search-triple");
        await SeedAccountAsync(accountId, ct);
        await SeedFileAsync(accountId, "/a/photo.jpg", 2048L, ct);
        await SeedFileAsync(accountId, "/b/photo.jpg", 2048L, ct);
        await SeedFileAsync(accountId, "/c/photo.jpg", 2048L, ct);

        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var criteria = SyncedItemSearchCriteriaFactory.Create(accountId, duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, ct);

        results.Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_duplicates_exist_for_one_account_they_do_not_appear_in_another_accounts_results()
    {
        var ct = TestContext.Current.CancellationToken;
        var accountWithDuplicates = new AccountId("dup-search-isolation-a");
        var accountWithoutDuplicates = new AccountId("dup-search-isolation-b");
        await SeedAccountAsync(accountWithDuplicates, ct);
        await SeedAccountAsync(accountWithoutDuplicates, ct);
        await SeedFileAsync(accountWithDuplicates, "/x/file.pdf",    999L, ct);
        await SeedFileAsync(accountWithDuplicates, "/y/file.pdf",    999L, ct);
        await SeedFileAsync(accountWithoutDuplicates, "/z/other.pdf", 999L, ct);

        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var criteria = SyncedItemSearchCriteriaFactory.Create(accountWithoutDuplicates, duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, ct);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_folders_share_size_and_name_they_are_excluded_from_duplicate_detection()
    {
        var ct = TestContext.Current.CancellationToken;
        var accountId = new AccountId("dup-search-folders-excluded");
        await SeedAccountAsync(accountId, ct);
        await SeedFolderAsync(accountId, "/a/Documents", ct);
        await SeedFolderAsync(accountId, "/b/Documents", ct);

        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var criteria = SyncedItemSearchCriteriaFactory.Create(accountId, duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, ct);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_items_have_null_size_they_are_excluded_from_duplicate_detection()
    {
        var ct = TestContext.Current.CancellationToken;
        var accountId = new AccountId("dup-search-null-size");
        await SeedAccountAsync(accountId, ct);
        await SeedFileAsync(accountId, "/a/unknown.bin", null, ct);
        await SeedFileAsync(accountId, "/b/unknown.bin", null, ct);

        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var criteria = SyncedItemSearchCriteriaFactory.Create(accountId, duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, ct);

        results.ShouldBeEmpty();
    }

    private async Task SeedAccountAsync(AccountId accountId, CancellationToken ct)
    {
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync(ct);
        context.Set<AccountEntity>().Add(new AccountEntity { Id = accountId });
        await context.SaveChangesAsync(ct);
    }

    private async Task SeedFileAsync(AccountId accountId, string remotePath, long? sizeInBytes, CancellationToken ct)
    {
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync(ct);
        context.Set<SyncedItemEntity>().Add(new SyncedItemEntity
        {
            AccountId       = accountId,
            RemoteItemId    = new OneDriveItemId(Guid.NewGuid().ToString()),
            RemoteParentId  = string.Empty,
            RemotePath      = remotePath,
            LocalPath       = string.Empty,
            IsFolder        = false,
            SizeInBytes     = sizeInBytes,
            RemoteModifiedAt = DateTimeOffset.UtcNow,
            Tags            = VersionInfoFactory.Create(Option.None<string>(), Option.None<string>())
        });
        await context.SaveChangesAsync(ct);
    }

    private async Task SeedFolderAsync(AccountId accountId, string remotePath, CancellationToken ct)
    {
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync(ct);
        context.Set<SyncedItemEntity>().Add(new SyncedItemEntity
        {
            AccountId        = accountId,
            RemoteItemId     = new OneDriveItemId(Guid.NewGuid().ToString()),
            RemoteParentId   = string.Empty,
            RemotePath       = remotePath,
            LocalPath        = string.Empty,
            IsFolder         = true,
            SizeInBytes      = null,
            RemoteModifiedAt = DateTimeOffset.UtcNow,
            Tags             = VersionInfoFactory.Create(Option.None<string>(), Option.None<string>())
        });
        await context.SaveChangesAsync(ct);
    }
}
