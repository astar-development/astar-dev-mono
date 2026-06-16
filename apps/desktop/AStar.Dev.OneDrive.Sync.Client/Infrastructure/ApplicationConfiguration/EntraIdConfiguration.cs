namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;

public record EntraIdConfiguration
{
    internal static string SectionName => "EntraId";

    public required string RedirectUri { get; init; }
    public required string ClientId { get; init; }
    public required IReadOnlyList<string> Scopes { get; init; }
    public required string AuthorityForMicrosoftAccountsOnly { get; init; }
}
