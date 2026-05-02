using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenALocalChangeDetector
{
    private const string AccountId = "acc-test-1";
    private const string BasePath  = "/sync-root";

    private static LocalChangeDetector CreateSut(MockFileSystem mockFs) => new(mockFs);

    private static SyncRuleEntity Rule(string remotePath, RuleType type) => new() { RemotePath = remotePath, RuleType = type };

    private static Dictionary<string, SyncedItemEntity> EmptyLookup() => [];

    [Fact]
    public void when_no_rules_are_provided_then_returns_empty_list()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(BasePath);
        var sut = CreateSut(mockFs);

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, [], EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_only_exclude_rules_are_provided_then_returns_empty_list()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddDirectory($"{BasePath}/Documents");
        mockFs.AddFile($"{BasePath}/Documents/file.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Exclude) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_include_rule_maps_to_non_existent_directory_then_returns_empty_list()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(BasePath);
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/NonExistentFolder", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_include_rule_directory_exists_but_is_empty_then_returns_empty_list()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddDirectory($"{BasePath}/Documents");
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_upload_job_is_created()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/report.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_has_correct_account_id()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/report.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].AccountId.ShouldBe(AccountId);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_has_correct_local_path()
    {
        const string filePath = $"{BasePath}/Documents/report.txt";
        var mockFs = new MockFileSystem();
        mockFs.AddFile(filePath, new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].LocalPath.ShouldBe(filePath);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_has_correct_relative_path()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/report.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].RelativePath.ShouldBe("Documents/report.txt");
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_direction_is_upload()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/report.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].Direction.ShouldBe(SyncDirection.Upload);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_download_url_equals_relative_path()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/report.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].DownloadUrl.ShouldBe(jobs[0].RelativePath);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_folder_id_is_empty()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/report.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].FolderId.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_known_file_last_write_is_within_five_second_tolerance_then_file_is_skipped()
    {
        const string filePath = $"{BasePath}/Documents/unchanged.txt";
        var lastWrite = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var fileData = new MockFileData("data") { LastWriteTime = lastWrite };
        var mockFs = new MockFileSystem();
        mockFs.AddFile(filePath, fileData);
        var remoteModified = new DateTimeOffset(lastWrite, TimeSpan.Zero);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = remoteModified, RemoteItemId = new OneDriveItemId("remote-id-1") }
        };
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, lookup);

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_known_file_last_write_is_beyond_five_second_tolerance_then_upload_job_is_created()
    {
        const string filePath = $"{BasePath}/Documents/modified.txt";
        var lastWrite = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var fileData = new MockFileData("data") { LastWriteTime = lastWrite };
        var mockFs = new MockFileSystem();
        mockFs.AddFile(filePath, fileData);
        var staleRemoteModified = new DateTimeOffset(lastWrite, TimeSpan.Zero).AddSeconds(-10);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = staleRemoteModified, RemoteItemId = new OneDriveItemId("remote-id-2") }
        };
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, lookup);

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_known_file_is_modified_then_job_remote_item_id_matches_known_entity()
    {
        const string knownRemoteItemId = "remote-id-known";
        const string filePath = $"{BasePath}/Documents/modified.txt";
        var lastWrite = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var fileData = new MockFileData("data") { LastWriteTime = lastWrite };
        var mockFs = new MockFileSystem();
        mockFs.AddFile(filePath, fileData);
        var staleRemoteModified = new DateTimeOffset(lastWrite, TimeSpan.Zero).AddSeconds(-10);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = staleRemoteModified, RemoteItemId = new OneDriveItemId(knownRemoteItemId) }
        };
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, lookup);

        jobs[0].RemoteItemId.ShouldBe(knownRemoteItemId);
    }

    [Fact]
    public void when_file_name_starts_with_dot_then_file_is_skipped()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/.hidden-dotfile", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_tmp_extension_then_file_is_skipped()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/download.tmp", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_temp_extension_then_file_is_skipped()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/download.temp", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_partial_extension_then_file_is_skipped()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/download.partial", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_subdirectory_name_starts_with_dot_then_files_inside_are_not_scanned()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/.hidden-sub/secret.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_subdirectory_matches_include_rule_then_files_inside_are_scanned()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/Reports/q1.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_subdirectory_matches_include_rule_then_job_relative_path_reflects_subdirectory()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/Reports/q1.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].RelativePath.ShouldBe("Documents/Reports/q1.txt");
    }

    [Fact]
    public void when_subdirectory_is_not_matched_by_any_rule_then_files_inside_are_not_scanned()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/Private/confidential.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[]
        {
            Rule("/Documents", RuleType.Include),
            Rule("/Documents/Private", RuleType.Exclude)
        };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_remote_path_does_not_match_any_sync_rule_then_file_is_skipped()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/report.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Photos", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_include_rule_has_multi_level_remote_path_then_local_path_starts_with_base_path()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/2024/report.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents/2024", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
        jobs[0].LocalPath.ShouldStartWith(BasePath);
    }

    [Fact]
    public void when_include_rule_has_multi_level_remote_path_then_local_path_contains_full_relative_structure()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile($"{BasePath}/Documents/2024/report.txt", new MockFileData("data"));
        var sut = CreateSut(mockFs);
        var rules = new[] { Rule("/Documents/2024", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
        jobs[0].RelativePath.ShouldBe("Documents/2024/report.txt");
    }
}
