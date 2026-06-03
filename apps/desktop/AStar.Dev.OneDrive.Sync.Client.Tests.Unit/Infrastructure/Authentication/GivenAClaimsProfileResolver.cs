using System.Security.Claims;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Authentication;

public sealed class GivenAClaimsProfileResolver
{
    private const string Fallback = "fallback-value";

    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims));

    [Fact]
    public void when_resolve_display_name_is_called_with_null_claims_then_fallback_is_returned() =>
        ClaimsProfileResolver.ResolveDisplayName(null, Fallback).ShouldBe(Fallback);

    [Fact]
    public void when_resolve_display_name_is_called_with_populated_name_claim_then_name_claim_value_is_returned() =>
        ClaimsProfileResolver.ResolveDisplayName(PrincipalWith(new Claim("name", "Jason Barden")), Fallback).ShouldBe("Jason Barden");

    [Fact]
    public void when_resolve_display_name_is_called_with_empty_name_claim_then_fallback_is_returned() =>
        ClaimsProfileResolver.ResolveDisplayName(PrincipalWith(new Claim("name", string.Empty)), Fallback).ShouldBe(Fallback);

    [Fact]
    public void when_resolve_display_name_is_called_with_no_name_claim_then_fallback_is_returned() =>
        ClaimsProfileResolver.ResolveDisplayName(PrincipalWith(new Claim("email", "user@example.com")), Fallback).ShouldBe(Fallback);

    [Fact]
    public void when_resolve_email_is_called_with_null_claims_then_fallback_is_returned() =>
        ClaimsProfileResolver.ResolveEmail(null, Fallback).ShouldBe(Fallback);

    [Fact]
    public void when_resolve_email_is_called_with_preferred_username_claim_then_preferred_username_is_returned() =>
        ClaimsProfileResolver.ResolveEmail(PrincipalWith(new Claim("preferred_username", "preferred@example.com")), Fallback).ShouldBe("preferred@example.com");

    [Fact]
    public void when_resolve_email_is_called_with_email_claim_only_then_email_is_returned() =>
        ClaimsProfileResolver.ResolveEmail(PrincipalWith(new Claim("email", "email@example.com")), Fallback).ShouldBe("email@example.com");

    [Fact]
    public void when_resolve_email_is_called_with_both_claims_then_preferred_username_takes_precedence() =>
        ClaimsProfileResolver.ResolveEmail(PrincipalWith(new Claim("preferred_username", "preferred@example.com"), new Claim("email", "email@example.com")), Fallback).ShouldBe("preferred@example.com");

    [Fact]
    public void when_resolve_email_is_called_with_empty_preferred_username_and_populated_email_then_fallback_is_returned() =>
        ClaimsProfileResolver.ResolveEmail(PrincipalWith(new Claim("preferred_username", string.Empty), new Claim("email", "email@example.com")), Fallback).ShouldBe(Fallback);

    [Fact]
    public void when_resolve_email_is_called_with_no_matching_claims_then_fallback_is_returned() =>
        ClaimsProfileResolver.ResolveEmail(PrincipalWith(new Claim("name", "Jason Barden")), Fallback).ShouldBe(Fallback);
}
