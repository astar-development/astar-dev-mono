# Refactor `AuthService.BuildSuccess` — Reduce Complexity + Add Tests

## Context

`AuthService.BuildSuccess` (line 131–149) is a private static method with cyclomatic complexity of 18.
It does two distinct things: resolves display-name and email from MSAL claims (with multiple null/empty
fallbacks), then assembles the `Result`. Being private it cannot be tested directly. The fix is to extract
the claims-resolution logic into a new `internal static class`, leaving `BuildSuccess` as a thin
coordinator, and testing the new class directly (InternalsVisibleTo is already configured).

## What changes

### 1. New file — `ClaimsProfileResolver.cs`

**Path:** `apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Authentication/ClaimsProfileResolver.cs`

`internal static class` with two methods:

```csharp
internal static string ResolveDisplayName(ClaimsPrincipal? claims, string fallback) =>
    claims?.FindFirst("name")?.Value is { Length: > 0 } name ? name : fallback;

internal static string ResolveEmail(ClaimsPrincipal? claims, string fallback)
{
    var candidate = claims?.FindFirst("preferred_username")?.Value
                    ?? claims?.FindFirst("email")?.Value;

    return string.IsNullOrEmpty(candidate) ? fallback : candidate;
}
```

Each method has CC ≤ 3.

### 2. Simplify `AuthService.BuildSuccess`

Replace the existing body with calls to the new resolver:

```csharp
private static Result<AuthResult, AuthError> BuildSuccess(AuthenticationResult result)
{
    var displayName = ClaimsProfileResolver.ResolveDisplayName(result.ClaimsPrincipal, result.Account.Username);
    var email = ClaimsProfileResolver.ResolveEmail(result.ClaimsPrincipal, result.Account.Username);

    return AuthResultFactory.Success(result.AccessToken, result.Account.HomeAccountId.Identifier, AccountProfileFactory.Create(displayName, email), result.ExpiresOn);
}
```

CC drops to ≤ 3.

### 3. New test file — `GivenAClaimsProfileResolver.cs`

**Path:** `apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Infrastructure/Authentication/GivenAClaimsProfileResolver.cs`

Namespace: `AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Authentication`
Class: `public sealed class GivenAClaimsProfileResolver`

`ResolveDisplayName` branches to cover:
| # | Scenario | Expected |
|---|----------|----------|
| 1 | null claims | fallback |
| 2 | claims with populated "name" claim | name claim value |
| 3 | claims with empty string "name" claim | fallback |
| 4 | claims present but no "name" claim at all | fallback |

`ResolveEmail` branches to cover:
| # | Scenario | Expected |
|---|----------|----------|
| 5 | null claims | fallback |
| 6 | claims with "preferred_username" only | preferred_username value |
| 7 | claims with "email" only (no preferred_username) | email value |
| 8 | claims with both — "preferred_username" takes precedence | preferred_username value |
| 9 | empty "preferred_username" + populated "email" | email value |
| 10 | claims present but neither claim exists | fallback |

Use `new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", value)]))` to build test principals inline.
Follow existing pattern: `Given<Context>` class, `when_[action]_then_[outcome]` methods, Shouldly assertions, AAA.

## Files NOT changed

- `AuthError.cs`, `AuthResult.cs`, `AuthResultFactory.cs`, `AccountProfileFactory.cs` — no changes needed.
- `GivenAnAuthService.cs`, `GivenAnAuthResultFactory.cs` — no changes needed (existing coverage of public surface unchanged).

## Verification

1. `dotnet build` → zero errors, zero warnings.
2. `dotnet test --filter "FullyQualifiedName~GivenAClaimsProfileResolver"` → all 10 tests pass.
3. `dotnet test` → no regressions across full suite.
