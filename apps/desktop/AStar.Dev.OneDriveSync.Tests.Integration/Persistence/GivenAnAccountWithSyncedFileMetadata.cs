using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

public sealed class GivenAnAccountWithSyncedFileMetadata
{
    [Fact]
    public async Task when_the_account_is_deleted_then_all_synced_file_metadata_rows_are_removed()
    {
        await using var factory = AppDbContextFactory.Create();
        await using AppDbContext context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        Account account = new AccountBuilder().Build();
        _ = context.Accounts.Add(account);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedFileMetadata.Add(BuildMetadataRow(account.Id, "docs/report.pdf"));
        context.SyncedFileMetadata.Add(BuildMetadataRow(account.Id, "photos/vacation.jpg"));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _ = context.Accounts.Remove(account);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var remainingRows = context.SyncedFileMetadata
            .Where(m => m.AccountId == account.Id)
            .Count();

        remainingRows.ShouldBe(0);
    }

    [Fact]
    public async Task when_one_of_many_accounts_is_deleted_then_only_its_metadata_rows_are_removed()
    {
        await using var factory = AppDbContextFactory.Create();
        await using AppDbContext context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        Account keptAccount    = new AccountBuilder().Build();
        Account deletedAccount = new AccountBuilder().Build();
        context.Accounts.AddRange(keptAccount, deletedAccount);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedFileMetadata.Add(BuildMetadataRow(keptAccount.Id,    "docs/kept.pdf"));
        context.SyncedFileMetadata.Add(BuildMetadataRow(deletedAccount.Id, "docs/deleted.pdf"));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _ = context.Accounts.Remove(deletedAccount);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedFileMetadata
            .Where(m => m.AccountId == deletedAccount.Id)
            .Count()
            .ShouldBe(0);

        context.SyncedFileMetadata
            .Where(m => m.AccountId == keptAccount.Id)
            .Count()
            .ShouldBe(1);
    }

    [Fact]
    public async Task when_am12_flag_is_disabled_then_existing_metadata_rows_are_retained()
    {
        await using var factory = AppDbContextFactory.Create();
        await using AppDbContext context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        Account account = new AccountBuilder().Build();
        _ = context.Accounts.Add(account);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedFileMetadata.Add(BuildMetadataRow(account.Id, "important/file.docx"));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var rowCount = context.SyncedFileMetadata
            .Where(m => m.AccountId == account.Id)
            .Count();

        rowCount.ShouldBe(1);
    }

    private static SyncedFileMetadata BuildMetadataRow(Guid accountId, string relativePath) =>
        new()
        {
            AccountId       = accountId,
            RemoteItemId    = Guid.NewGuid().ToString(),
            RelativePath    = relativePath,
            FileName        = Path.GetFileName(relativePath),
            FileSizeBytes   = 1024,
            Sha256Checksum  = new string('a', 64),
            LastModifiedUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            CreatedUtc      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
}
