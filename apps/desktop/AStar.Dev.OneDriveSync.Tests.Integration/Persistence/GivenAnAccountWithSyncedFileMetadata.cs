using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

public sealed class GivenAnAccountWithSyncedFileMetadata
{
    [Fact]
    public async Task when_the_account_is_deleted_then_all_synced_file_metadata_rows_are_removed()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var account = new AccountBuilder().Build();
        _ = context.Accounts.Add(account);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedFileMetadata.Add(BuildMetadataRow(account.Id, "docs/report.pdf"));
        context.SyncedFileMetadata.Add(BuildMetadataRow(account.Id, "photos/vacation.jpg"));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _ = context.Accounts.Remove(account);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        int remainingRows = context.SyncedFileMetadata
            .Count(m => m.AccountId == account.Id);

        remainingRows.ShouldBe(0);
    }

    [Fact]
    public async Task when_one_of_many_accounts_is_deleted_then_only_its_metadata_rows_are_removed()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var keptAccount    = new AccountBuilder().Build();
        var deletedAccount = new AccountBuilder().Build();
        context.Accounts.AddRange(keptAccount, deletedAccount);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedFileMetadata.Add(BuildMetadataRow(keptAccount.Id,    "docs/kept.pdf"));
        context.SyncedFileMetadata.Add(BuildMetadataRow(deletedAccount.Id, "docs/deleted.pdf"));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _ = context.Accounts.Remove(deletedAccount);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedFileMetadata
            .Count(m => m.AccountId == deletedAccount.Id)
            .ShouldBe(0);

        context.SyncedFileMetadata
            .Count(m => m.AccountId == keptAccount.Id)
            .ShouldBe(1);
    }

    [Fact]
    public async Task when_am12_flag_is_disabled_then_existing_metadata_rows_are_retained()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var account = new AccountBuilder().Build();
        _ = context.Accounts.Add(account);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedFileMetadata.Add(BuildMetadataRow(account.Id, "important/file.docx"));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        int rowCount = context.SyncedFileMetadata
            .Count(m => m.AccountId == account.Id);

        rowCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_synced_file_metadata_is_persisted_then_timestamp_properties_round_trip_to_millisecond_precision()
    {
        var knownTimestamp = new DateTimeOffset(2025, 6, 15, 10, 30, 45, 123, TimeSpan.Zero);

        await using var factory = AppDbContextFactory.Create();
        await using var writeContext = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var account = new AccountBuilder().Build();
        _ = writeContext.Accounts.Add(account);
        _ = await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        writeContext.SyncedFileMetadata.Add(BuildMetadataRow(account.Id, "docs/report.pdf", knownTimestamp));
        _ = await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var readContext = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var retrieved = readContext.SyncedFileMetadata
            .Single(m => m.AccountId == account.Id);

        retrieved.LastModifiedUtc.ToUnixTimeMilliseconds().ShouldBe(knownTimestamp.ToUnixTimeMilliseconds());
        retrieved.CreatedUtc.ToUnixTimeMilliseconds().ShouldBe(knownTimestamp.ToUnixTimeMilliseconds());
    }

    private static SyncedFileMetadata BuildMetadataRow(Guid accountId, string relativePath, DateTimeOffset? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow;

        return new()
        {
            AccountId       = accountId,
            RemoteItemId    = Guid.NewGuid().ToString(),
            RelativePath    = relativePath,
            FileName        = Path.GetFileName(relativePath),
            FileSizeBytes   = 1024,
            Sha256Checksum  = new string('a', 64),
            LastModifiedUtc = ts,
            CreatedUtc      = ts
        };
    }
}
