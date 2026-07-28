using System.ComponentModel.DataAnnotations;

namespace AStar.Dev.Velopack.Publishing;

/// <summary>Configuration for checking application updates published as GitHub Releases via Velopack.</summary>
public record VelopackUpdateSettings
{
    /// <summary>The configuration section name this record binds to.</summary>
    public static string SectionName => "Updates";

    /// <summary>The GitHub repository releases are published to, e.g. https://github.com/owner/repo.</summary>
    [Required]
    public required Uri GithubRepositoryUrl { get; init; }

    /// <summary>
    /// The app-specific prefix for the Velopack update channel, e.g. "clock" or "onedrive-sync".
    /// Combined with the current platform (linux/win/osx) to form the explicit channel checked
    /// against GitHub Releases. Required because this mono-repo hosts every desktop app's releases
    /// in one GitHub repository, and Velopack's channel lookup is scoped per-repo, not per-packId -
    /// apps sharing a bare channel name (e.g. "linux") can pick up each other's releases.
    /// </summary>
    [Required]
    public required string ChannelPrefix { get; init; }
}
