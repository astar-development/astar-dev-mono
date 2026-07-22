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
}
