using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAFileAuditor : IDisposable
{
    private readonly FileAuditFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => fixture.Dispose();

    [Fact]
    public async Task when_the_local_folder_holds_included_files_then_they_are_counted_without_folders()
    {
        fixture.Include("Docs");
        fixture.Write("Docs/a.txt");
        fixture.Write("Docs/sub/b.txt");
        fixture.Write("Other/c.txt");
        fixture.Feed.EnqueueLast();

        var counts = await fixture.CountsAsync(CancellationToken);

        counts.LocalFiles.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_local_folder_holds_dot_folders_and_app_temp_files_then_they_are_not_counted()
    {
        fixture.Include("Docs");
        fixture.Write("Docs/a.txt");
        fixture.Write("Docs/.git/config");
        fixture.Write($"Docs/b.txt.{Guid.NewGuid():D}.download");
        fixture.Feed.EnqueueLast();

        var counts = await fixture.CountsAsync(CancellationToken);

        counts.LocalFiles.ShouldBe(1);
    }

    [Fact]
    public async Task when_rules_overlap_then_a_local_file_is_counted_once()
    {
        fixture.Include("Docs", "Docs/Sub");
        fixture.Write("Docs/Sub/a.txt");
        fixture.Write("Docs/b.txt");
        fixture.Feed.EnqueueLast();

        var counts = await fixture.CountsAsync(CancellationToken);

        counts.LocalFiles.ShouldBe(2);
    }

    [Fact]
    public async Task when_no_folder_is_selected_then_nothing_is_counted()
    {
        fixture.Write("Docs/a.txt");
        fixture.Feed.EnqueueLast(ScriptedChangeFeed.Folder("docs", "root", "Docs"), ScriptedChangeFeed.File("a", "docs", "a.txt"));

        var counts = await fixture.CountsAsync(CancellationToken);

        (counts.LocalFiles, counts.CloudFiles).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_the_cloud_has_included_files_across_pages_then_files_are_counted_without_folders()
    {
        fixture.Include("Docs");
        fixture.Feed.EnqueueMore("page-2", ScriptedChangeFeed.Folder("docs", "root", "Docs"), ScriptedChangeFeed.File("a", "docs", "a.txt"));
        fixture.Feed.EnqueueLast(ScriptedChangeFeed.Folder("sub", "docs", "Sub"), ScriptedChangeFeed.File("b", "sub", "b.txt"), ScriptedChangeFeed.Folder("other", "root", "Other"), ScriptedChangeFeed.File("c", "other", "c.txt"));

        var counts = await fixture.CountsAsync(CancellationToken);

        counts.CloudFiles.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_cloud_child_arrives_before_its_parent_folder_then_it_is_still_judged_by_its_path()
    {
        fixture.Include("Docs");
        fixture.Feed.EnqueueMore("page-2", ScriptedChangeFeed.File("b", "sub", "b.txt"));
        fixture.Feed.EnqueueLast(ScriptedChangeFeed.Folder("sub", "docs", "Sub"), ScriptedChangeFeed.Folder("docs", "root", "Docs"));

        var counts = await fixture.CountsAsync(CancellationToken);

        counts.CloudFiles.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_cloud_folder_differs_only_in_case_from_the_rule_then_its_files_are_counted()
    {
        fixture.Include("docs");
        fixture.Feed.EnqueueLast(ScriptedChangeFeed.Folder("docs", "root", "DOCS"), ScriptedChangeFeed.File("a", "docs", "a.txt"));

        var counts = await fixture.CountsAsync(CancellationToken);

        counts.CloudFiles.ShouldBe(1);
    }

    [Fact]
    public async Task when_rules_overlap_then_a_cloud_file_is_counted_once()
    {
        fixture.Include("Docs", "Docs/Sub");
        fixture.Feed.EnqueueLast(ScriptedChangeFeed.Folder("docs", "root", "Docs"), ScriptedChangeFeed.Folder("sub", "docs", "Sub"), ScriptedChangeFeed.File("a", "sub", "a.txt"));

        var counts = await fixture.CountsAsync(CancellationToken);

        counts.CloudFiles.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_cloud_file_is_deleted_or_reported_twice_then_it_is_counted_by_its_last_report()
    {
        fixture.Include("Docs");
        fixture.Feed.EnqueueMore("page-2", ScriptedChangeFeed.Folder("docs", "root", "Docs"), ScriptedChangeFeed.File("a", "docs", "a.txt"), ScriptedChangeFeed.File("b", "docs", "b.txt"));
        fixture.Feed.EnqueueLast(ScriptedChangeFeed.Deleted("a", "docs", "a.txt"), ScriptedChangeFeed.File("b", "docs", "b.txt"));

        var counts = await fixture.CountsAsync(CancellationToken);

        counts.CloudFiles.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_cloud_holds_dot_folders_and_app_temp_files_then_they_are_not_counted()
    {
        fixture.Include("Docs");
        fixture.Feed.EnqueueLast(
            ScriptedChangeFeed.Folder("docs", "root", "Docs"),
            ScriptedChangeFeed.Folder("git", "docs", ".git"),
            ScriptedChangeFeed.File("config", "git", "config"),
            ScriptedChangeFeed.File("temp", "docs", $"x.txt.{Guid.NewGuid():D}.download"),
            ScriptedChangeFeed.File("a", "docs", "a.txt"));

        var counts = await fixture.CountsAsync(CancellationToken);

        counts.CloudFiles.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_cloud_is_read_then_it_starts_with_no_cursor_and_follows_the_next_links_only()
    {
        fixture.Include("Docs");
        fixture.Feed.EnqueueMore("page-2");
        fixture.Feed.EnqueueLast();

        _ = await fixture.CountsAsync(CancellationToken);

        fixture.Feed.Requested.Select(cursor => cursor.Match(found => found.Value, () => "none")).ShouldBe(["none", "page-2"]);
    }

    [Fact]
    public async Task when_the_cloud_cannot_be_read_then_the_audit_fails_with_the_provider_error()
    {
        fixture.Include("Docs");
        fixture.Feed.Enqueue(Result.Failure<RemoteChangePage, ProviderError>(ProviderErrorFactory.CreateUnexpected()));

        var result = await fixture.Auditor.AuditAsync(FileAuditFixture.Account, CancellationToken);

        result.Match(_ => string.Empty, failure => failure.GetType().Name + ((FileAuditFailure.CloudUnavailable)failure).Error.GetType().Name).ShouldBe($"{nameof(FileAuditFailure.CloudUnavailable)}{nameof(ProviderError.Unexpected)}");
    }

    [Fact]
    public async Task when_the_feed_repeats_a_link_then_the_audit_stops_with_a_failure()
    {
        fixture.Include("Docs");
        fixture.Feed.EnqueueMore("page-2");
        fixture.Feed.EnqueueMore("page-2");
        fixture.Feed.EnqueueMore("page-2");

        var result = await fixture.Auditor.AuditAsync(FileAuditFixture.Account, CancellationToken);

        result.Match(_ => string.Empty, failure => failure.GetType().Name).ShouldBe(nameof(FileAuditFailure.CloudUnavailable));
    }

    [Fact]
    public async Task when_the_account_has_no_sync_root_then_the_audit_fails_as_root_unavailable()
    {
        fixture.Roots.Root = Option.None<System.IO.Abstractions.IDirectoryInfo>();
        fixture.Feed.EnqueueLast();

        var result = await fixture.Auditor.AuditAsync(FileAuditFixture.Account, CancellationToken);

        result.Match(_ => string.Empty, failure => failure.GetType().Name).ShouldBe(nameof(FileAuditFailure.RootUnavailable));
    }

    [Fact]
    public async Task when_the_sync_root_folder_is_missing_then_the_audit_fails_as_root_unavailable()
    {
        fixture.FileSystem.Directory.Delete(FileAuditFixture.Root, true);
        fixture.Feed.EnqueueLast();

        var result = await fixture.Auditor.AuditAsync(FileAuditFixture.Account, CancellationToken);

        result.Match(_ => string.Empty, failure => failure.GetType().Name).ShouldBe(nameof(FileAuditFailure.RootUnavailable));
    }

    [Fact]
    public async Task when_the_rules_cannot_be_read_then_the_audit_fails_as_rules_unavailable()
    {
        fixture.Rules.Listing = Result.Failure<IReadOnlyList<SelectionRule>, ErrorResponse>(new ErrorResponse("unreadable"));
        fixture.Feed.EnqueueLast();

        var result = await fixture.Auditor.AuditAsync(FileAuditFixture.Account, CancellationToken);

        result.Match(_ => string.Empty, failure => failure.GetType().Name).ShouldBe(nameof(FileAuditFailure.RulesUnavailable));
    }

    [Fact]
    public async Task when_the_audit_is_cancelled_then_the_cancellation_is_thrown()
    {
        fixture.Include("Docs");
        fixture.Feed.EnqueueLast();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var audit = () => fixture.Auditor.AuditAsync(FileAuditFixture.Account, source.Token);

        await Should.ThrowAsync<OperationCanceledException>(audit);
    }

    [Fact]
    public async Task when_the_audit_logs_then_no_file_or_folder_name_reaches_the_log()
    {
        fixture.Include("Secret");
        fixture.Write("Secret/Payroll.txt");
        fixture.Feed.EnqueueLast(ScriptedChangeFeed.Folder("secret", "root", "Secret"), ScriptedChangeFeed.File("payroll", "secret", "Payroll.txt"));

        _ = await fixture.CountsAsync(CancellationToken);

        fixture.Logs.Logger.Messages.ShouldNotBeEmpty();
        fixture.Logs.Logger.Messages.ShouldAllBe(message => !message.Contains("Payroll", StringComparison.OrdinalIgnoreCase) && !message.Contains("Secret", StringComparison.OrdinalIgnoreCase));
    }
}
