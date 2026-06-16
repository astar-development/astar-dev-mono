namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;

public record EntraIdConfiguration(string RedirectUri, string ClientId, IReadOnlyList<string> Scopes, string AuthorityForMicrosoftAccountsOnly)
{
internal static string SectionName => "EntraId";
}
