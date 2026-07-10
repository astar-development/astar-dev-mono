using AStar.Dev.Wallpaper.Scraper.Support;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Support;

public sealed class GivenChromeCookieExtractor
{
    [Fact]
    public void when_home_directory_contains_supported_profiles_then_returns_candidate_paths_in_preferred_order()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string? originalConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        try
        {
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);

            string googleChromeProfile = Path.Combine(homeDirectory, ".config", "google-chrome", "Default");
            string chromiumProfile = Path.Combine(homeDirectory, ".config", "chromium", "Profile 1");
            Directory.CreateDirectory(googleChromeProfile);
            Directory.CreateDirectory(chromiumProfile);
            File.WriteAllText(Path.Combine(googleChromeProfile, "Cookies"), "dummy");
            File.WriteAllText(Path.Combine(chromiumProfile, "Cookies"), "dummy");

            var candidates = ChromeCookieExtractor.FindCookieDatabasePaths(homeDirectory);

            candidates.ShouldContain(Path.Combine(googleChromeProfile, "Cookies"));
            candidates.ShouldContain(Path.Combine(chromiumProfile, "Cookies"));
            candidates[0].ShouldBe(Path.Combine(googleChromeProfile, "Cookies"));
        }
        finally
        {
            if (Directory.Exists(homeDirectory))
            {
                Directory.Delete(homeDirectory, recursive: true);
            }

            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", originalConfigHome);
        }
    }
}
