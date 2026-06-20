using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;
using Microsoft.Extensions.Logging;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Detection;

public sealed class GivenALocalChangeDetector
{
    private const string AccountId = "acc-test-1";
    private const string BasePath = "/sync-root";

    private static LocalChangeDetector CreateSut(MockFileSystem mockFileSystem) => new(mockFileSystem, Substitute.For<ILogger<LocalChangeDetector>>());

    private static SyncRuleEntity Rule(string remotePath, RuleType type) => new() { RemotePath = remotePath, RuleType = type };

    private static Dictionary<string, SyncedItemEntity> EmptyLookup() => [];

    [Fact]
    public void when_no_rules_are_provided_then_returns_empty_list()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory(BasePath);
        var sut = CreateSut(mockFileSystem);

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, [], EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_only_exclude_rules_are_provided_then_returns_empty_list()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory($"{BasePath}/Documents");
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/file.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Exclude) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_include_rule_maps_to_non_existent_directory_then_returns_empty_list()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory(BasePath);
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/NonExistentFolder", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_include_rule_directory_exists_but_is_empty_then_returns_empty_list()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory($"{BasePath}/Documents");
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_upload_job_is_created()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/report.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_has_correct_account_id()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/report.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].Remote.AccountId.Id.ShouldBe(AccountId);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_has_correct_local_path()
    {
        const string filePath = $"{BasePath}/Documents/report.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(filePath).Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].Target.LocalPath.ShouldBe(filePath);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_has_correct_relative_path()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/report.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].Target.RelativePath.ShouldBe("Documents/report.txt");
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_is_upload_sync_job()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/report.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].ShouldBeOfType<UploadSyncJob>();
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_folder_id_is_root()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/report.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].Remote.FolderId.Id.ShouldBe("root");
    }

    [Fact]
    public void when_known_file_is_modified_then_job_folder_id_is_root()
    {
        const string filePath = $"{BasePath}/Documents/modified.txt";
        var lastWrite = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(filePath).Which(m => m.HasStringContent("data"));
        mockFileSystem.File.SetLastWriteTime(filePath, lastWrite);
        var staleRemoteModified = new DateTimeOffset(lastWrite, TimeSpan.Zero).AddSeconds(-10);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = staleRemoteModified, RemoteItemId = new OneDriveItemId("remote-id-folder-test") }
        };
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, lookup);

        jobs[0].Remote.FolderId.Id.ShouldBe("root");
    }

    [Fact]
    public void when_known_file_last_write_is_within_five_second_tolerance_then_file_is_skipped()
    {
        const string filePath = $"{BasePath}/Documents/unchanged.txt";
        var lastWrite = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(filePath).Which(m => m.HasStringContent("data"));
        mockFileSystem.File.SetLastWriteTime(filePath, lastWrite);
        var remoteModified = new DateTimeOffset(lastWrite, TimeSpan.Zero);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = remoteModified, RemoteItemId = new OneDriveItemId("remote-id-1") }
        };
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, lookup);

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_known_file_last_write_is_beyond_five_second_tolerance_then_upload_job_is_created()
    {
        const string filePath = $"{BasePath}/Documents/modified.txt";
        var lastWrite = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(filePath).Which(m => m.HasStringContent("data"));
        mockFileSystem.File.SetLastWriteTime(filePath, lastWrite);
        var staleRemoteModified = new DateTimeOffset(lastWrite, TimeSpan.Zero).AddSeconds(-10);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = staleRemoteModified, RemoteItemId = new OneDriveItemId("remote-id-2") }
        };
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, lookup);

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_known_file_last_write_exactly_equals_remote_modified_plus_five_seconds_then_file_is_skipped()
    {
        const string filePath = $"{BasePath}/Documents/boundary.txt";
        var remoteModifiedAt = new DateTimeOffset(2025, 3, 15, 9, 0, 0, TimeSpan.Zero);
        var lastWrite = remoteModifiedAt.AddSeconds(5).UtcDateTime;
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(filePath).Which(m => m.HasStringContent("data"));
        mockFileSystem.File.SetLastWriteTimeUtc(filePath, lastWrite);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = remoteModifiedAt, RemoteItemId = new OneDriveItemId("remote-id-boundary") }
        };
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, lookup);

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_is_zero_bytes_and_not_in_lookup_then_upload_job_is_created()
    {
        const string filePath = $"{BasePath}/Documents/empty.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(filePath).Which(m => m.HasStringContent(string.Empty));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_known_file_is_modified_then_job_remote_item_id_matches_known_entity()
    {
        const string knownRemoteItemId = "remote-id-known";
        const string filePath = $"{BasePath}/Documents/modified.txt";
        var lastWrite = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(filePath).Which(m => m.HasStringContent("data"));
        mockFileSystem.File.SetLastWriteTime(filePath, lastWrite);
        var staleRemoteModified = new DateTimeOffset(lastWrite, TimeSpan.Zero).AddSeconds(-10);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = staleRemoteModified, RemoteItemId = new OneDriveItemId(knownRemoteItemId) }
        };
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, lookup);

        jobs[0].Remote.RemoteItemId.Id.ShouldBe(knownRemoteItemId);
    }

    [Fact]
    public void when_file_name_starts_with_dot_then_file_is_skipped()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/.hidden-dotfile").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_tmp_extension_then_file_is_skipped()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/download.tmp").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_temp_extension_then_file_is_skipped()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/download.temp").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_partial_extension_then_file_is_skipped()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/download.partial").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_download_extension_then_file_is_skipped()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/0023.jpg.f027d572fc8f4f79b66c3bb0f9b7d155.download").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_subdirectory_name_starts_with_dot_then_files_inside_are_not_scanned()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/.hidden-sub/secret.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_subdirectory_matches_include_rule_then_files_inside_are_scanned()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/Reports/q1.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_subdirectory_matches_include_rule_then_job_relative_path_reflects_subdirectory()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/Reports/q1.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs[0].Target.RelativePath.ShouldBe("Documents/Reports/q1.txt");
    }

    [Fact]
    public void when_subdirectory_is_not_matched_by_any_rule_then_files_inside_are_not_scanned()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/Private/confidential.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
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
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/report.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Photos", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_include_rule_has_multi_level_remote_path_then_local_path_starts_with_base_path()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/2024/report.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents/2024", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
        jobs[0].Target.LocalPath.ShouldStartWith(BasePath);
    }

    [Fact]
    public void when_include_rule_has_multi_level_remote_path_then_local_path_contains_full_relative_structure()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile($"{BasePath}/Documents/2024/report.txt").Which(m => m.HasStringContent("data"));
        var sut = CreateSut(mockFileSystem);
        var rules = new[] { Rule("/Documents/2024", RuleType.Include) };

        var jobs = sut.DetectNewAndModifiedFiles(AccountId, BasePath, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
        jobs[0].Target.RelativePath.ShouldBe("Documents/2024/report.txt");
    }
}
