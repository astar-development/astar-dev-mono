# S007 — Authentication & Token Management

**Phase:** MVP  
**Area:** packages/infra/astar-dev-onedrive-client — Features/Authentication  
**Spec refs:** AU-01 to AU-05, NF-06

---

## User Story

As a user,  
I want to authenticate my personal Microsoft account once via the system browser and have the app silently refresh my token afterwards,  
So that I never have to log in again unless my password changes or I revoke access.

---

## Acceptance Criteria

### MSAL OAuth Flow (AU-01)
- [ ] MSAL `PublicClientApplication` configured for `consumers` tenant only — no Entra ID / work accounts
- [ ] Authentication triggered via system browser (`WithDefaultRedirectUri()` or local loopback redirect)
- [ ] Required Graph scopes requested at auth time: `Files.ReadWrite`, `offline_access` (minimum)
- [ ] `IMsalClient` interface in `Features/Authentication/`; `MsalClient` is the implementation

### Token Persistence (AU-02, NF-06)
- [ ] `ITokenManager` interface + `TokenManager` implementation
- [ ] On Linux: attempt OS keychain (libsecret / KDE Wallet via MSAL cache helper); if unavailable, fall back to encrypted local store
- [ ] Insecure fallback (AU-03): before using the local encrypted store, display an explicit opt-in consent dialog; user must confirm; decision stored per account in SQLite
- [ ] Fallback store uses a machine-scoped encryption key — never plaintext (NF-06)
- [ ] `IConsentStore` interface records per-account consent decision

### Silent Token Refresh (AU-04)
- [ ] `TokenManager.GetTokenSilentlyAsync()` attempts silent MSAL refresh before every sync
- [ ] Silent refresh failure does **not** throw — returns `Result<AccessToken>.Failure(TokenRefreshError)`

### Token Refresh Failure Handling (AU-05)
- [ ] When silent refresh fails: all syncs for the affected account are paused immediately
- [ ] A persistent banner/badge is shown on the account in the Accounts view and Dashboard until the user re-authenticates
- [ ] Re-authentication triggers the system browser flow (same as initial auth)
- [ ] `IAuthStateService` publishes an observable `AccountAuthStateChanged` that the UI subscribes to

### Tests
- [ ] **Unit test**: `TokenManager.GetTokenSilentlyAsync()` — MSAL returns expired token → returns `Failure`
- [ ] **Unit test**: `TokenManager` — silent refresh succeeds → returns `Success<AccessToken>`
- [ ] **Unit test**: `ConsentStore` — consent stored and retrieved correctly per account
- [ ] **Unit test**: `AuthStateService` — token refresh failure transitions account to `AuthRequired` state
- [ ] Mock MSAL using `IMsalClient` interface — no real HTTP calls in unit tests (NF-14)
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- MSAL `Microsoft.Identity.Client` package — version in `Directory.Packages.props`
- `ISecureTokenStore` interface in the desktop app (platform-specific); implementation stays in app, not in the `OneDrive.Client` package
- NF-16: all `TokenManager` methods return `Result<T>` or `Option<T>`
- NF-00: authentication events (success, failure, consent decision) logged at `Information`/`Warning`; token contents **never** logged
- `ITokenManager` is registered as **scoped per account** — each account has its own token lifecycle

---

## Implementation Constraints

- **`ConfigureAwait(false)` on all `await` calls** — `IMsalClient`, `ITokenManager`, and `IConsentStore` are library-layer types; every `await` must use `.ConfigureAwait(false)` to avoid capturing the Avalonia `SynchronizationContext` and risk deadlocking the UI thread.
- **MSAL browser flow off the UI thread** — `AcquireTokenInteractive` must not be called on the Avalonia UI thread. Dispatch to a background thread via `Task.Run`, then marshal the result back to the UI thread via `.ObserveOn(RxApp.MainThreadScheduler)` before updating bound properties.
- **Auth state observable subscribers use `ObserveOn`** — any ViewModel subscribing to `IAuthStateService.AccountAuthStateChanged` must call `.ObserveOn(RxApp.MainThreadScheduler)` before mutating any property or collection, as the observable fires from the token-refresh background thread.
- **Dispose subscriptions explicitly** — auth state subscriptions stored in ViewModels must be disposed on VM deactivation (store the `IDisposable` from `.Subscribe()` and call `.Dispose()` in the relevant lifecycle hook) to prevent memory leaks across account add/remove cycles.
---

## Dependencies

- S001 (project scaffolding — `AStar.Dev.OneDrive.Client` package)
- S002 (database — consent decisions stored in SQLite)
