using System.Text.Json;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Localization;

public sealed class GivenTheEnGbJsonFile
{
    [Fact]
    public void when_read_then_dashboard_all_synced_key_is_present()
    {
        var assembly = typeof(LocalizationService).Assembly;
        using var stream = assembly.GetManifestResourceStream("AStarDev.OneDriveSyncClient.Assets.Localization.en-GB.json");
        stream.ShouldNotBeNull();
        using var document = JsonDocument.Parse(stream);

        document.RootElement.TryGetProperty("Dashboard.AllSynced", out _).ShouldBeTrue();
    }
}
