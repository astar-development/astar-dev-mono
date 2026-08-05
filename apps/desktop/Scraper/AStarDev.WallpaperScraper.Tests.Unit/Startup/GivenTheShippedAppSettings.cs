using System.Runtime.CompilerServices;
using AStar.Dev.Velopack.Publishing;
using AStarDev.Utilities;
using Microsoft.Extensions.Configuration;

namespace AStarDev.WallpaperScraper.Tests.Unit.Startup;

public class GivenTheShippedAppSettings
{
    [Fact]
    public void when_loaded_then_update_channel_prefix_matches_the_release_workflow_channel()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(ProductionAppSettingsPath())
            .Build();

        var channelPrefix = configuration[$"{VelopackUpdateSettings.SectionName}:ChannelPrefix"];

        channelPrefix.ShouldBe("wallpaper-scraper");
    }

    private static string ProductionAppSettingsPath([CallerFilePath] string testFilePath = "") =>
        Directory.GetParent(testFilePath)!.Parent!.Parent!.FullName.CombinePath("AStarDev.WallpaperScraper", "appsettings.json");
}
