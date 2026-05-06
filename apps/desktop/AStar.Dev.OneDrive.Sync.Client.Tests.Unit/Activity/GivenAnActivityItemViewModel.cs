using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Activity;

public sealed class GivenAnActivityItemViewModel
{
    private const string AccountIdValue = "account-123";
    private const string AccountEmailValue = "user@example.com";
    private const string FolderNameValue = "Documents";
    private const string RelativePathValue = "Documents/report.pdf";
    private const string FileNameValue = "report.pdf";
    private const string ErrorMessageValue = "Network timeout";
    private const long FileSizeValue = 2048L;

    private static SyncJob BuildSyncJob(SyncDirection direction = SyncDirection.Download, string relativePath = RelativePathValue, DateTimeOffset? completedAt = null, string? errorMessage = null)
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("", relativePath);
        var metadata = SyncFileMetadataFactory.Create(FileSizeValue, default);

        SyncJob baseJob = direction switch
        {
            SyncDirection.Download => SyncJobFactory.CreateDownload(remote, target, metadata),
            SyncDirection.Upload   => SyncJobFactory.CreateUpload(remote, target, metadata),
            SyncDirection.Delete   => SyncJobFactory.CreateDelete(remote, target, metadata),
            _                      => SyncJobFactory.CreateDownload(remote, target, metadata)
        };

        var status = baseJob.Status with { CompletedAt = completedAt, ErrorMessage = errorMessage };

        return baseJob with { Status = status };
    }

    private static SyncConflict BuildSyncConflict(string relativePath = RelativePathValue, DateTimeOffset? detectedAt = null) => new()
    {
        Remote = RemoteItemRefFactory.Create(new AccountId(AccountIdValue), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)),
        RelativePath = relativePath,
        DetectedAt = detectedAt ?? DateTimeOffset.UtcNow
    };

    [Fact]
    public void when_type_is_downloaded_then_type_label_returns_downloaded() =>
        new ActivityItemViewModel { Type = ActivityItemType.Downloaded }.TypeLabel.ShouldBe("downloaded");

    [Fact]
    public void when_type_is_uploaded_then_type_label_returns_uploaded() =>
        new ActivityItemViewModel { Type = ActivityItemType.Uploaded }.TypeLabel.ShouldBe("uploaded");

    [Fact]
    public void when_type_is_deleted_then_type_label_returns_deleted() =>
        new ActivityItemViewModel { Type = ActivityItemType.Deleted }.TypeLabel.ShouldBe("deleted");

    [Fact]
    public void when_type_is_conflict_then_type_label_returns_conflict() =>
        new ActivityItemViewModel { Type = ActivityItemType.Conflict }.TypeLabel.ShouldBe("conflict");

    [Fact]
    public void when_type_is_error_then_type_label_returns_error() =>
        new ActivityItemViewModel { Type = ActivityItemType.Error }.TypeLabel.ShouldBe("error");

    [Fact]
    public void when_type_is_info_then_type_label_returns_info() =>
        new ActivityItemViewModel { Type = ActivityItemType.Info }.TypeLabel.ShouldBe("info");

    [Fact]
    public void when_type_is_downloaded_then_type_icon_returns_down_arrow() =>
        new ActivityItemViewModel { Type = ActivityItemType.Downloaded }.TypeIcon.ShouldBe("↓");

    [Fact]
    public void when_type_is_uploaded_then_type_icon_returns_up_arrow() =>
        new ActivityItemViewModel { Type = ActivityItemType.Uploaded }.TypeIcon.ShouldBe("↑");

    [Fact]
    public void when_type_is_deleted_then_type_icon_returns_multiplication_sign() =>
        new ActivityItemViewModel { Type = ActivityItemType.Deleted }.TypeIcon.ShouldBe("×");

    [Fact]
    public void when_type_is_conflict_then_type_icon_returns_warning_sign() =>
        new ActivityItemViewModel { Type = ActivityItemType.Conflict }.TypeIcon.ShouldBe("⚠");

    [Fact]
    public void when_type_is_error_then_type_icon_returns_warning_sign() =>
        new ActivityItemViewModel { Type = ActivityItemType.Error }.TypeIcon.ShouldBe("⚠");

    [Fact]
    public void when_type_is_info_then_type_icon_returns_bullet() =>
        new ActivityItemViewModel { Type = ActivityItemType.Info }.TypeIcon.ShouldBe("•");

    [Fact]
    public void when_occurred_at_is_30_seconds_ago_then_time_ago_text_returns_just_now()
    {
        var sut = new ActivityItemViewModel { OccurredAt = DateTimeOffset.UtcNow.AddSeconds(-30) };

        sut.TimeAgoText.ShouldBe("just now");
    }

    [Fact]
    public void when_occurred_at_is_5_minutes_ago_then_time_ago_text_returns_minutes_ago()
    {
        var sut = new ActivityItemViewModel { OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-5) };

        sut.TimeAgoText.ShouldBe("5m ago");
    }

    [Fact]
    public void when_occurred_at_is_3_hours_ago_then_time_ago_text_returns_hours_ago()
    {
        var sut = new ActivityItemViewModel { OccurredAt = DateTimeOffset.UtcNow.AddHours(-3) };

        sut.TimeAgoText.ShouldBe("3h ago");
    }

    [Fact]
    public void when_occurred_at_is_25_hours_ago_then_time_ago_text_returns_yesterday()
    {
        var sut = new ActivityItemViewModel { OccurredAt = DateTimeOffset.UtcNow.AddHours(-25) };

        sut.TimeAgoText.ShouldBe("yesterday");
    }

    [Fact]
    public void when_occurred_at_is_5_days_ago_then_time_ago_text_returns_days_ago()
    {
        var sut = new ActivityItemViewModel { OccurredAt = DateTimeOffset.UtcNow.AddDays(-5) };

        sut.TimeAgoText.ShouldBe("5d ago");
    }

    [Fact]
    public void when_file_size_is_non_zero_then_file_size_text_returns_non_empty_string()
    {
        var sut = new ActivityItemViewModel { FileSize = FileSizeValue };

        sut.FileSizeText.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void when_file_size_is_zero_then_file_size_text_returns_non_null_string()
    {
        var sut = new ActivityItemViewModel { FileSize = 0L };

        sut.FileSizeText.ShouldNotBeNull();
    }

    [Fact]
    public void when_job_direction_is_download_then_from_job_returns_downloaded_type()
    {
        var job = BuildSyncJob(direction: SyncDirection.Download);

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.Type.ShouldBe(ActivityItemType.Downloaded);
    }

    [Fact]
    public void when_job_direction_is_upload_then_from_job_returns_uploaded_type()
    {
        var job = BuildSyncJob(direction: SyncDirection.Upload);

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.Type.ShouldBe(ActivityItemType.Uploaded);
    }

    [Fact]
    public void when_job_direction_is_delete_then_from_job_returns_deleted_type()
    {
        var job = BuildSyncJob(direction: SyncDirection.Delete);

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.Type.ShouldBe(ActivityItemType.Deleted);
    }

    [Fact]
    public void when_from_job_is_called_then_account_id_is_mapped_from_job()
    {
        var job = BuildSyncJob();

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.AccountId.ShouldBe(AccountIdValue);
    }

    [Fact]
    public void when_from_job_is_called_then_account_email_is_mapped_from_parameter()
    {
        var job = BuildSyncJob();

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.AccountEmail.ShouldBe(AccountEmailValue);
    }

    [Fact]
    public void when_from_job_is_called_then_folder_name_is_mapped_from_parameter()
    {
        var job = BuildSyncJob();

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.FolderName.ShouldBe(FolderNameValue);
    }

    [Fact]
    public void when_from_job_is_called_then_file_name_is_derived_from_relative_path()
    {
        var job = BuildSyncJob(relativePath: RelativePathValue);

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.FileName.ShouldBe(FileNameValue);
    }

    [Fact]
    public void when_from_job_is_called_then_relative_path_is_mapped_from_job()
    {
        var job = BuildSyncJob(relativePath: RelativePathValue);

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.RelativePath.ShouldBe(RelativePathValue);
    }

    [Fact]
    public void when_from_job_is_called_then_file_size_is_mapped_from_job()
    {
        var job = BuildSyncJob();

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.FileSize.ShouldBe(FileSizeValue);
    }

    [Fact]
    public void when_from_job_is_called_then_error_message_is_mapped_from_job()
    {
        var job = BuildSyncJob(errorMessage: ErrorMessageValue);

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.ErrorMessage.ShouldBe(ErrorMessageValue);
    }

    [Fact]
    public void when_job_completed_at_is_set_then_occurred_at_equals_job_completed_at()
    {
        var completedAt = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var job = BuildSyncJob(completedAt: completedAt);

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        result.OccurredAt.ShouldBe(completedAt);
    }

    [Fact]
    public void when_job_completed_at_is_null_then_occurred_at_is_approximately_utc_now()
    {
        var before = DateTimeOffset.UtcNow;
        var job = BuildSyncJob(completedAt: null);

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue, FolderNameValue);

        var after = DateTimeOffset.UtcNow;
        result.OccurredAt.ShouldBeInRange(before, after);
    }

    [Fact]
    public void when_from_job_is_called_without_folder_name_then_folder_name_defaults_to_empty_string()
    {
        var job = BuildSyncJob();

        var result = ActivityItemViewModel.FromJob(job, AccountEmailValue);

        result.FolderName.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_from_conflict_is_called_then_type_is_conflict()
    {
        var conflict = BuildSyncConflict();

        var result = ActivityItemViewModel.FromConflict(conflict, AccountEmailValue, FolderNameValue);

        result.Type.ShouldBe(ActivityItemType.Conflict);
    }

    [Fact]
    public void when_from_conflict_is_called_then_account_id_is_mapped_from_conflict()
    {
        var conflict = BuildSyncConflict();

        var result = ActivityItemViewModel.FromConflict(conflict, AccountEmailValue, FolderNameValue);

        result.AccountId.ShouldBe(AccountIdValue);
    }

    [Fact]
    public void when_from_conflict_is_called_then_account_email_is_mapped_from_parameter()
    {
        var conflict = BuildSyncConflict();

        var result = ActivityItemViewModel.FromConflict(conflict, AccountEmailValue, FolderNameValue);

        result.AccountEmail.ShouldBe(AccountEmailValue);
    }

    [Fact]
    public void when_from_conflict_is_called_then_folder_name_is_mapped_from_parameter()
    {
        var conflict = BuildSyncConflict();

        var result = ActivityItemViewModel.FromConflict(conflict, AccountEmailValue, FolderNameValue);

        result.FolderName.ShouldBe(FolderNameValue);
    }

    [Fact]
    public void when_from_conflict_is_called_then_file_name_is_derived_from_relative_path()
    {
        var conflict = BuildSyncConflict(relativePath: RelativePathValue);

        var result = ActivityItemViewModel.FromConflict(conflict, AccountEmailValue, FolderNameValue);

        result.FileName.ShouldBe(FileNameValue);
    }

    [Fact]
    public void when_from_conflict_is_called_then_relative_path_is_mapped_from_conflict()
    {
        var conflict = BuildSyncConflict(relativePath: RelativePathValue);

        var result = ActivityItemViewModel.FromConflict(conflict, AccountEmailValue, FolderNameValue);

        result.RelativePath.ShouldBe(RelativePathValue);
    }

    [Fact]
    public void when_from_conflict_is_called_then_occurred_at_is_mapped_from_detected_at()
    {
        var detectedAt = new DateTimeOffset(2026, 3, 10, 8, 0, 0, TimeSpan.Zero);
        var conflict = BuildSyncConflict(detectedAt: detectedAt);

        var result = ActivityItemViewModel.FromConflict(conflict, AccountEmailValue, FolderNameValue);

        result.OccurredAt.ShouldBe(detectedAt);
    }

    [Fact]
    public void when_error_factory_is_called_then_type_is_error()
    {
        var result = ActivityItemViewModel.Error(AccountIdValue, AccountEmailValue, ErrorMessageValue);

        result.Type.ShouldBe(ActivityItemType.Error);
    }

    [Fact]
    public void when_error_factory_is_called_then_account_id_is_set_correctly()
    {
        var result = ActivityItemViewModel.Error(AccountIdValue, AccountEmailValue, ErrorMessageValue);

        result.AccountId.ShouldBe(AccountIdValue);
    }

    [Fact]
    public void when_error_factory_is_called_then_account_email_is_set_correctly()
    {
        var result = ActivityItemViewModel.Error(AccountIdValue, AccountEmailValue, ErrorMessageValue);

        result.AccountEmail.ShouldBe(AccountEmailValue);
    }

    [Fact]
    public void when_error_factory_is_called_then_error_message_is_set_correctly()
    {
        var result = ActivityItemViewModel.Error(AccountIdValue, AccountEmailValue, ErrorMessageValue);

        result.ErrorMessage.ShouldBe(ErrorMessageValue);
    }

    [Fact]
    public void when_error_factory_is_called_then_file_name_is_sync_error()
    {
        var result = ActivityItemViewModel.Error(AccountIdValue, AccountEmailValue, ErrorMessageValue);

        result.FileName.ShouldBe("Sync error");
    }
}
