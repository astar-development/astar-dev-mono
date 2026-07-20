using System.ComponentModel.DataAnnotations;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;

/// <summary>Configuration for checking application updates published as GitHub Releases.</summary>
public record UpdateSettings
{
    internal static string SectionName => "Updates";

    /// <summary>The GitHub repository releases are published to, e.g. https://github.com/owner/repo.</summary>
    [Required(AllowEmptyStrings = false)]
    public required string GithubRepositoryUrl { get; init; }
}
