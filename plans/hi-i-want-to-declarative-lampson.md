# CloudSync — Add Account (MVP, OneDrive / personal MSA only)

## Context
CloudSync is UI-only: `PlaceholderSyncActions.AddAccountAsync` only logs; accounts come from `PlaceholderSyncOverviewSource` (random Guids). No MSAL/Graph/DB yet. First real feature = add OneDrive personal account(s), written fresh (no reuse of OneDriveSyncClient code or DB), with detailed, PII-free logging of every step.

## Current OneDriveSyncClient flow (analysed)
- 3-step wizard (SignIn → SelectFolders → Confirm), no logger in wizard VM.
- MSAL PCA, authority `consumers`, loopback `http://localhost`, system browser, `Prompt.SelectAccount`; scopes `User.Read Files.ReadWrite.All offline_access`.
- Token cache: libsecret keyring; on failure **silent plaintext fallback** (`WithLinuxUnprotectedFile`), only a Warn log.
- Profile from ID-token claims, no `/me`. Drive id fetched in-memory only, never persisted.
- Persist: SQLite upsert by `HomeAccountId`. **Re-add overwrites row** (loses custom sync path, quota), adds duplicate cards.
- Default local path hard-codes two emails to `/run/media/jbarden/Tbdrive/...`.
- Wizard closes before DB write → persist failure invisible to user; MSAL account orphaned.
- Folder load uses `CancellationToken.None`; Graph errors swallowed to `Fail(ex.Message)`, never logged; hard-coded English errors.
- Cancellation not logged. No initial sync on add.

## Proposed approach
### Domain (Core/Domain)
- Reuse `AccountId`, `CloudAccount`, `CloudProvider.OneDrive`. Add `AccountAddOutcome` union: `Added | ReAuthenticated | Cancelled | Failed(AccountAddFailure)`; `AccountAddFailure` union: `BrowserUnavailable | AuthRejected | Network | TokenCacheUnavailable | DriveUnavailable | PersistFailed`.
- Store provider identity separately: `ProviderAccountKey` (MSAL `HomeAccountId.Identifier`) + `DriveId`, both strong types.

### Infrastructure
- `IAccountAuthenticator` (MSAL): one DI-registered `IPublicClientApplication`, `MsalCacheHelper` registered once (thread-safe, `Lazy<Task>`). Config bound from `appsettings.json` (new `CloudSync` client id or reuse existing app registration — see questions).
- After sign-in: `GET /me/drive` (drive id, owner, quota) — persist drive id.
- `IAccountStore`: new SQLite via EF Core (central versions exist, `Directory.Packages.props:127-135`).
- Duplicate check by `ProviderAccountKey` **before** write; re-add = refresh tokens, keep settings.
- Persist **before** closing the dialog; on failure remove MSAL account (rollback) and show error.

### Logging (core ask)
- New `[LoggerMessage]` templates (CloudSync-specific, EventId range e.g. 5000-5099) via `AStar.Dev.Logging.Extensions` pattern.
- One `AccountAddAttemptId` (Guid) per attempt in a logging scope; every event carries it plus `Provider`.
- Events: AttemptStarted, TokenCacheInitialised(mode: Keyring|Fallback), BrowserLaunched, AuthSucceeded(durationMs, scopesGranted), AuthCancelled, AuthFailed(MsalErrorCode, correlationId), DriveFetched(durationMs, driveType), DuplicateDetected, Persisted(durationMs), RollbackPerformed, AttemptCompleted(outcome, totalDurationMs).
- **No PII** (LG-02, past #492): never email/display name/tokens/drive name; account key only as short SHA-256 hash.
- Forward MSAL internal logs (`WithLogging`, `enablePiiLogging:false`) at Warning+ (Verbose when debug toggle).
- MSAL `CorrelationId` logged so failures can be matched with Microsoft-side logs.

### UI
- Add-account dialog VM (no Avalonia in VM), wired to existing `AddAccountCommand` path via `ISyncActions.AddAccountAsync`.
- Cancel button cancels the MSAL call (real CancellationToken end-to-end).
- All strings via `AStarDev.Localization`.

## Phases (one GH issue each)
1. Domain types + outcome unions + factories (TDD).
2. SQLite account store + duplicate detection (integration tests incl. casing variant + concurrent add).
3. MSAL authenticator + token cache + `/me/drive` (interfaces faked in tests).
4. Logging templates + attempt scope (tests via `RecordingLogger`, assert no PII).
5. Add-account dialog VM + view + replace placeholder source for accounts (snapshot tests).

## Verification
- `dotnet clean && dotnet build --no-restore` zero warnings.
- Scoped `dotnet test` per touched test project.
- Manual: add account in real app, cancel, re-add same account, add second account; check Aspire dashboard (OTLP `http://localhost:4317`) for full event trail with one attempt id and no PII.

## Decisions (Cap'n, 2026-11-05)
- Keyring missing → consent dialog; plaintext only on yes, else add cancelled (`TokenCacheUnavailable`). Consent choice logged (not the path).
- Re-add same MSA → re-authenticate, keep all settings, show "already added" notice; outcome `ReAuthenticated`.
- Scope: sign-in + drive/quota + save only. Sync pairs = separate later feature.
- Storage: new CloudSync SQLite via EF Core.
- Default (not asked): reuse existing Entra app registration client id `3057f494-...` (consumers, loopback) via `appsettings.json`; change later if separate registration wanted.
