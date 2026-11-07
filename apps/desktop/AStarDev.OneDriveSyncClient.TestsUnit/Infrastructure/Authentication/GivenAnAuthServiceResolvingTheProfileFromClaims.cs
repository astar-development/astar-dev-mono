using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Authentication;

public sealed class GivenAnAuthServiceResolvingTheProfileFromClaims
{
    private const string Authority = "https://login.microsoftonline.com/consumers";
    private const string NameClaim = "name";
    private const string PreferredUsernameClaim = "preferred_username";
    private const string EmailClaim = "email";

    private static readonly IReadOnlyList<string> Scopes = ["Files.ReadWrite", "User.Read"];

    private static async Task<(AccountProfile Profile, string Username)> SignInWithClaimsAsync(params (string Type, string Value)[] claims)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var app = PublicClientApplicationBuilder.Create(FakeMsalTokenEndpoint.ClientId)
            .WithAuthority(Authority)
            .WithInstanceDiscovery(false)
            .WithHttpClientFactory(new FakeMsalTokenEndpoint(claims.ToDictionary(claim => claim.Type, claim => claim.Value)))
            .Build();
        _ = await ((IByRefreshToken)app).AcquireTokenByRefreshToken(Scopes, "seed-refresh-token").ExecuteAsync(cancellationToken);
        string username = (await app.GetAccountsAsync()).Single().Username;
        var options = Options.Create(new EntraIdConfiguration { ClientId = FakeMsalTokenEndpoint.ClientId, RedirectUri = "http://localhost", Scopes = Scopes, AuthorityForMicrosoftAccountsOnly = Authority });
        var sut = new AuthService(app, Substitute.For<ITokenCacheService>(), options, Substitute.For<ILogger<AuthService>>());

        var result = await sut.AcquireTokenSilentAsync(FakeMsalTokenEndpoint.HomeAccountId, cancellationToken);

        return (result.ShouldBeOfType<Ok<AuthResult, AuthError>>().Value.Profile, username);
    }

    [Fact]
    public async Task when_the_name_claim_is_populated_then_the_display_name_is_the_name_claim()
    {
        var (profile, _) = await SignInWithClaimsAsync((NameClaim, "Ada Lovelace"), (PreferredUsernameClaim, "ada@example.com"));

        profile.DisplayName.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public async Task when_the_name_claim_is_empty_then_the_display_name_falls_back_to_the_account_username()
    {
        var (profile, username) = await SignInWithClaimsAsync((NameClaim, string.Empty), (PreferredUsernameClaim, "ada@example.com"));

        profile.DisplayName.ShouldBe(username);
    }

    [Fact]
    public async Task when_there_is_no_name_claim_then_the_display_name_falls_back_to_the_account_username()
    {
        var (profile, username) = await SignInWithClaimsAsync((PreferredUsernameClaim, "ada@example.com"));

        profile.DisplayName.ShouldBe(username);
    }

    [Fact]
    public async Task when_the_preferred_username_claim_is_present_then_the_email_is_the_preferred_username()
    {
        var (profile, _) = await SignInWithClaimsAsync((PreferredUsernameClaim, "preferred@example.com"));

        profile.Email.ShouldBe("preferred@example.com");
    }

    [Fact]
    public async Task when_only_the_email_claim_is_present_then_the_email_is_the_email_claim()
    {
        var (profile, _) = await SignInWithClaimsAsync((EmailClaim, "email@example.com"));

        profile.Email.ShouldBe("email@example.com");
    }

    [Fact]
    public async Task when_both_claims_are_present_then_the_preferred_username_takes_precedence()
    {
        var (profile, _) = await SignInWithClaimsAsync((PreferredUsernameClaim, "preferred@example.com"), (EmailClaim, "email@example.com"));

        profile.Email.ShouldBe("preferred@example.com");
    }

    [Fact]
    public async Task when_the_preferred_username_is_empty_and_the_email_is_populated_then_the_email_falls_back_to_the_account_username()
    {
        var (profile, username) = await SignInWithClaimsAsync((PreferredUsernameClaim, string.Empty), (EmailClaim, "email@example.com"));

        profile.Email.ShouldBe(username);
    }

    [Fact]
    public async Task when_there_are_no_email_claims_then_the_email_falls_back_to_the_account_username()
    {
        var (profile, username) = await SignInWithClaimsAsync((NameClaim, "Ada Lovelace"));

        profile.Email.ShouldBe(username);
    }
}
