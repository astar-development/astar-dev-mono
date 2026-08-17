using System.Text.Json;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Activity;

public sealed class GivenTheEnGbLocalisationFile
{
    private static readonly string JsonFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../apps/desktop/AStarDev.OneDriveSyncClient/Assets/Localization/en-GB.json"));

    [Fact]
    public void when_en_gb_json_is_read_then_activity_info_key_exists()
    {
        string json = File.ReadAllText(JsonFilePath);
        var document = JsonDocument.Parse(json);

        document.RootElement.TryGetProperty("Activity.Info", out _).ShouldBeTrue();
    }

    [Fact]
    public void when_en_gb_json_is_read_then_activity_sync_error_key_exists()
    {
        string json = File.ReadAllText(JsonFilePath);
        var document = JsonDocument.Parse(json);

        document.RootElement.TryGetProperty("Activity.SyncError", out _).ShouldBeTrue();
    }
}
