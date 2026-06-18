using System.Text.Json;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Assets;

public sealed class GivenTheEnGbJson
{
    private static readonly string JsonPath = Path.GetFullPath(
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..", "apps", "desktop", "AStar.Dev.OneDrive.Sync.Client", "Assets", "Localization", "en-GB.json"));

    private static JsonElement RootElement()
    {
        string json = File.ReadAllText(JsonPath);

        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public void when_read_then_ignore_description_key_exists() =>
        RootElement().TryGetProperty("ConflictPolicy.Ignore.Description", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_keep_both_description_key_exists() =>
        RootElement().TryGetProperty("ConflictPolicy.KeepBoth.Description", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_last_write_wins_description_key_exists() =>
        RootElement().TryGetProperty("ConflictPolicy.LastWriteWins.Description", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_local_wins_description_key_exists() =>
        RootElement().TryGetProperty("ConflictPolicy.LocalWins.Description", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_remote_wins_description_key_exists() =>
        RootElement().TryGetProperty("ConflictPolicy.RemoteWins.Description", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_files_exclude_tooltip_key_exists() =>
        RootElement().TryGetProperty("Files.Exclude.Tooltip", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_files_include_tooltip_key_exists() =>
        RootElement().TryGetProperty("Files.Include.Tooltip", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_dashboard_no_sync_path_key_exists() =>
        RootElement().TryGetProperty("Dashboard.NoSyncPath", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_dashboard_pending_key_exists() =>
        RootElement().TryGetProperty("Dashboard.Pending", out _).ShouldBeTrue();
}
