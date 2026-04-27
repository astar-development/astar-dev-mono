using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenALocalChangeDetector : IDisposable
{
    private const string AccountId = "acc-test-1";

    private readonly string _tempBase = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly LocalChangeDetector _sut = new();

    public GivenALocalChangeDetector() => Directory.CreateDirectory(_tempBase);

    public void Dispose()
    {
        if(Directory.Exists(_tempBase))
            Directory.Delete(_tempBase, recursive: true);
    }

    private static SyncRuleEntity Rule(string remotePath, RuleType type) => new() { RemotePath = remotePath, RuleType = type };

    private static Dictionary<string, SyncedItemEntity> EmptyLookup() => [];

    private string MakeDir(string relativePath)
    {
        string fullPath = Path.Combine(_tempBase, relativePath.TrimStart(Path.DirectorySeparatorChar));
        Directory.CreateDirectory(fullPath);

        return fullPath;
    }

    private string WriteFile(string relativeDir, string fileName, string content = "data")
    {
        string dir = Path.Combine(_tempBase, relativeDir.TrimStart(Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, fileName);
        File.WriteAllText(filePath, content);

        return filePath;
    }

    [Fact]
    public void when_no_rules_are_provided_then_returns_empty_list()
    {
        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, [], EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_only_exclude_rules_are_provided_then_returns_empty_list()
    {
        MakeDir("Documents");
        WriteFile("Documents", "file.txt");
        var rules = new[] { Rule("/Documents", RuleType.Exclude) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_include_rule_maps_to_non_existent_directory_then_returns_empty_list()
    {
        var rules = new[] { Rule("/NonExistentFolder", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_include_rule_directory_exists_but_is_empty_then_returns_empty_list()
    {
        MakeDir("Documents");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_upload_job_is_created()
    {
        string filePath = WriteFile("Documents", "report.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_has_correct_account_id()
    {
        WriteFile("Documents", "report.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs[0].AccountId.ShouldBe(AccountId);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_has_correct_local_path()
    {
        string filePath = WriteFile("Documents", "report.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs[0].LocalPath.ShouldBe(filePath);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_has_correct_relative_path()
    {
        WriteFile("Documents", "report.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs[0].RelativePath.ShouldBe("Documents/report.txt");
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_direction_is_upload()
    {
        WriteFile("Documents", "report.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs[0].Direction.ShouldBe(SyncDirection.Upload);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_download_url_equals_relative_path()
    {
        WriteFile("Documents", "report.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs[0].DownloadUrl.ShouldBe(jobs[0].RelativePath);
    }

    [Fact]
    public void when_new_file_is_not_in_lookup_then_job_folder_id_is_empty()
    {
        WriteFile("Documents", "report.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs[0].FolderId.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_known_file_last_write_is_within_five_second_tolerance_then_file_is_skipped()
    {
        string filePath = WriteFile("Documents", "unchanged.txt");
        var remoteModified = new DateTimeOffset(new FileInfo(filePath).LastWriteTimeUtc, TimeSpan.Zero);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = remoteModified, RemoteItemId = new OneDriveItemId("remote-id-1") }
        };
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, lookup);

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_known_file_last_write_is_beyond_five_second_tolerance_then_upload_job_is_created()
    {
        string filePath = WriteFile("Documents", "modified.txt");
        var staleRemoteModified = new DateTimeOffset(new FileInfo(filePath).LastWriteTimeUtc, TimeSpan.Zero).AddSeconds(-10);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = staleRemoteModified, RemoteItemId = new OneDriveItemId("remote-id-2") }
        };
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, lookup);

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_known_file_is_modified_then_job_remote_item_id_matches_known_entity()
    {
        const string knownRemoteItemId = "remote-id-known";
        string filePath = WriteFile("Documents", "modified.txt");
        var staleRemoteModified = new DateTimeOffset(new FileInfo(filePath).LastWriteTimeUtc, TimeSpan.Zero).AddSeconds(-10);
        var lookup = new Dictionary<string, SyncedItemEntity>
        {
            [filePath] = new() { RemoteModifiedAt = staleRemoteModified, RemoteItemId = new OneDriveItemId(knownRemoteItemId) }
        };
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, lookup);

        jobs[0].RemoteItemId.ShouldBe(knownRemoteItemId);
    }

    [Fact]
    public void when_file_has_hidden_attribute_then_file_is_skipped()
    {
        if(!OperatingSystem.IsWindows())
            return;
        string filePath = WriteFile("Documents", "hidden.txt");
        File.SetAttributes(filePath, FileAttributes.Hidden);
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_name_starts_with_dot_then_file_is_skipped()
    {
        WriteFile("Documents", ".hidden-dotfile");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_tmp_extension_then_file_is_skipped()
    {
        WriteFile("Documents", "download.tmp");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_temp_extension_then_file_is_skipped()
    {
        WriteFile("Documents", "download.temp");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_has_partial_extension_then_file_is_skipped()
    {
        WriteFile("Documents", "download.partial");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_subdirectory_name_starts_with_dot_then_files_inside_are_not_scanned()
    {
        WriteFile("Documents/.hidden-sub", "secret.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_subdirectory_matches_include_rule_then_files_inside_are_scanned()
    {
        WriteFile("Documents/Reports", "q1.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void when_subdirectory_matches_include_rule_then_job_relative_path_reflects_subdirectory()
    {
        WriteFile("Documents/Reports", "q1.txt");
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs[0].RelativePath.ShouldBe("Documents/Reports/q1.txt");
    }

    [Fact]
    public void when_subdirectory_is_not_matched_by_any_rule_then_files_inside_are_not_scanned()
    {
        WriteFile("Documents/Private", "confidential.txt");
        var rules = new[]
        {
            Rule("/Documents", RuleType.Include),
            Rule("/Documents/Private", RuleType.Exclude)
        };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }

    [Fact]
    public void when_file_remote_path_does_not_match_any_sync_rule_then_file_is_skipped()
    {
        WriteFile("Documents", "report.txt");
        var rules = new[] { Rule("/Photos", RuleType.Include) };

        var jobs = _sut.DetectNewAndModifiedFiles(AccountId, _tempBase, rules, EmptyLookup());

        jobs.ShouldBeEmpty();
    }
}
