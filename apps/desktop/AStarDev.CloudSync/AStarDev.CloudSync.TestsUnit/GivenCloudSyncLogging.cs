using AStarDev.CloudSync.Logging;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenCloudSyncLogging
{
    [Fact]
    public void when_the_folder_is_asked_then_it_is_a_logs_folder_inside_the_application_data_folder()
    {
        var folder = CloudSyncLogging.FolderPath();

        (Path.GetFileName(folder), folder.Contains("AStarDev.CloudSync", StringComparison.Ordinal)).ShouldBe(("logs", true));
    }

    [Fact]
    public void when_the_retention_is_asked_then_log_files_are_kept_for_two_weeks()
    {
        CloudSyncLogging.Retention.ShouldBe(TimeSpan.FromDays(14));
    }
}
