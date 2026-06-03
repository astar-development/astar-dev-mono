using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Localization;

public sealed class GivenTheEnGbJsonFile
{
    [Fact]
    public void when_read_then_dashboard_all_synced_key_is_present()
    {
        var assembly = typeof(LocalizationService).Assembly;
        using var stream = assembly.GetManifestResourceStream("AStar.Dev.OneDrive.Sync.Client.Assets.Localization.en-GB.json");
        stream.ShouldNotBeNull();
        using var document = JsonDocument.Parse(stream!);

        document.RootElement.TryGetProperty("Dashboard.AllSynced", out _).ShouldBeTrue();
    }
}
