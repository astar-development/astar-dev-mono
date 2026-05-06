# Data-Clump Analysis — AStar.Dev.OneDrive.Sync.Client

> Generated 2026-05-06. Scope: all `.cs` files in `apps/desktop/AStar.Dev.OneDrive.Sync.Client/`.
> The SyncJob data-clumps (`RemoteItemRef`, `SyncFileTarget`, `SyncFileMetadata`, `SyncJobStatus`) were already extracted per `syncjob-dataclump-refactor.md` and are not re-listed here.

---

## Summary

| # | Clump | Fields | Consumers | Status |
|---|-------|--------|-----------|--------|
| 1 | Account Profile | `DisplayName` · `Email` · `AccentIndex` | 5 | Not extracted |
| 2 | Account Sync Config | `ConflictPolicy` · `LocalSyncPath` | 4 | Not extracted |
| 3 | Storage Quota | `QuotaTotal` · `QuotaUsed` | 3 | Not extracted |
| 4 | Remote File Identity in SyncConflict | `AccountId` · `FolderId` · `RemoteItemId` | 2 | `RemoteItemRef` exists — not applied |
| 5 | Local File Location in SyncConflict | `LocalPath` · `RelativePath` | 2 | `SyncFileTarget` exists — not applied |
| 6 | Conflict Snapshot | `LocalModified` · `LocalSize` · `RemoteModified` · `RemoteSize` | 2 | Not extracted |
| 7 | Version Tags in SyncedItemEntity | `ETag` · `CTag` | 2 | `VersionInfo` exists — not applied |

---

## Clump 1 — Account Profile (5 consumers)

**Fields:** `DisplayName` (string), `Email` (string), `AccentIndex` (int)

**Question answered:** *Who is this Microsoft account and how should it be presented?*

| Consumer | File | Fields present |
|----------|------|----------------|
| `OneDriveAccount` | `Accounts/OneDriveAccount.cs:11-20` | All three |
| `AccountEntity` | `Data/Entities/AccountEntity.cs:9-11` | All three |
| `AuthResult` | `Infrastructure/Authentication/AuthResult.cs:7` | `DisplayName` + `Email` |
| `AccountCardViewModel` | `Accounts/AccountCardViewModel.cs:29-57` | Reads all three from `_model` |
| `DashboardAccountViewModel` | `Dashboard/DashboardAccountViewModel.cs:21-23` | Reads all three from `_account` |

**Pattern:** Every site that holds or displays account identity reads the same three fields together. `AccentIndex` always accompanies `DisplayName` + `Email` because all rendering sites need it to pick an avatar colour.

**Proposed extraction:**

```csharp
// Domain/AccountProfile.cs
public sealed record AccountProfile(string DisplayName, string Email, int AccentIndex);

// Domain/AccountProfileFactory.cs
public static class AccountProfileFactory
{
    public static AccountProfile Create(string displayName, string email, int accentIndex)
        => new(displayName, email, accentIndex);
}
```

**Migration impact:**
- `OneDriveAccount` — replace three flat properties with `AccountProfile Profile { get; set; }`
- `AccountEntity` — same; or use EF Core Owned Entity Type `[Owned]`
- `AuthResult` — drop `DisplayName` + `Email` constructor params; pass `AccountProfile` instead
- `AccountCardViewModel` — `_model.Profile.DisplayName`, `_model.Profile.Email`, `_model.Profile.AccentIndex`
- `DashboardAccountViewModel` — same access pattern

---

## Clump 2 — Account Sync Config (4 consumers)

**Fields:** `ConflictPolicy` (enum), `LocalSyncPath` (`LocalSyncPath` / string)

**Question answered:** *How should this account's sync behave locally?*

| Consumer | File | Fields present |
|----------|------|----------------|
| `OneDriveAccount` | `Accounts/OneDriveAccount.cs:44-49` | Both |
| `AccountEntity` | `Data/Entities/AccountEntity.cs:16-17` | Both |
| `AccountSettings` | `Accounts/AccountSettings.cs:12-18` | Both (LocalSyncPath as `string`) |
| `DashboardAccountViewModel.SyncNowAsync` | `Dashboard/DashboardAccountViewModel.cs:100-104` | Both (inline construction) |

**Pattern:** Every time an account is loaded, saved, or used to trigger a sync, these two fields move together. `AccountSettings` is a DTO that duplicates both, and `SyncNowAsync` inline-constructs an `OneDriveAccount` by copying only these two fields plus identity fields.

**Note:** `AccountSettings.LocalSyncPath` is still a raw `string` — it should use `LocalSyncPath` (the validated value object) for consistency.

**Proposed extraction:**

```csharp
// Domain/AccountSyncConfig.cs
public sealed record AccountSyncConfig(ConflictPolicy ConflictPolicy, LocalSyncPath LocalSyncPath);

// Domain/AccountSyncConfigFactory.cs
public static class AccountSyncConfigFactory
{
    public static AccountSyncConfig Create(ConflictPolicy policy, LocalSyncPath path)
        => new(policy, path);
}
```

**Migration impact:**
- `OneDriveAccount` — replace two flat properties with `AccountSyncConfig SyncConfig { get; set; }`
- `AccountEntity` — same; EF Core Owned Entity Type for `AccountSyncConfig`
- `AccountSettings` — replace with `AccountSyncConfig` directly; delete `AccountSettings`
- `DashboardAccountViewModel.SyncNowAsync` — pass `entity.SyncConfig` rather than copying fields
- All callers of `account.ConflictPolicy` → `account.SyncConfig.ConflictPolicy`
- All callers of `account.LocalSyncPath` → `account.SyncConfig.LocalSyncPath`

---

## Clump 3 — Storage Quota (3 consumers)

**Fields:** `QuotaTotal` (long, bytes), `QuotaUsed` (long, bytes)

**Question answered:** *How much OneDrive storage does this account have and use?*

| Consumer | File | Fields present |
|----------|------|----------------|
| `OneDriveAccount` | `Accounts/OneDriveAccount.cs:32-35` | Both |
| `AccountEntity` | `Data/Entities/AccountEntity.cs:13-14` | Both |
| `DashboardAccountViewModel` | `Dashboard/DashboardAccountViewModel.cs:26-33` | Both; derives `StorageFraction` + `StorageText` |

**Pattern:** `QuotaTotal` and `QuotaUsed` always appear and change together (refreshed from Graph API as a pair). `DashboardAccountViewModel` re-derives formatted display from both simultaneously.

**Proposed extraction:**

```csharp
// Domain/StorageQuota.cs
public sealed record StorageQuota(long TotalBytes, long UsedBytes)
{
    public double Fraction => TotalBytes > 0 ? Math.Clamp((double)UsedBytes / TotalBytes, 0, 1) : 0;
}

// Domain/StorageQuotaFactory.cs
public static class StorageQuotaFactory
{
    public static StorageQuota Create(long totalBytes, long usedBytes) => new(totalBytes, usedBytes);
    public static StorageQuota Unknown => new(0, 0);
}
```

**Migration impact:**
- `OneDriveAccount` — `QuotaTotal` + `QuotaUsed` → `StorageQuota Quota { get; set; }`
- `AccountEntity` — same; EF Core Owned Entity Type
- `DashboardAccountViewModel` — `StorageFraction` and `StorageText` delegate to `_account.Quota.Fraction`; remove inline calculation

---

## Clump 4 — Remote File Identity in SyncConflict (2 consumers)

**Fields:** `AccountId` (string), `FolderId` (string), `RemoteItemId` (string)

**Question answered:** *Which remote item is involved in this conflict?*

| Consumer | File | Fields present |
|----------|------|----------------|
| `SyncConflict` | `Domain/SyncConflict.cs:12-14` | All three — as raw strings |
| `RemoteItemRef` | `Domain/RemoteItemRef.cs` | Extracted record — used by `SyncJob` |

**Pattern:** `SyncConflict` uses the same three-field identity group as `SyncJob.Remote`, but stores raw strings instead of the already-extracted `RemoteItemRef`. This inconsistency means conflict resolution code handles the same concept twice with different shapes.

**Note:** `SyncConflict.AccountId` / `FolderId` / `RemoteItemId` use `string` not the strong-typed IDs (`AccountId`, `OneDriveFolderId`, `OneDriveItemId`). The fix should use the strong IDs and the existing record.

**Proposed change:** Replace the three string fields with `RemoteItemRef Remote { get; init; }`.

**Migration impact:**
- `Domain/SyncConflict.cs:12-14` — remove `AccountId`, `FolderId`, `RemoteItemId`; add `RemoteItemRef Remote`
- `Data/Repositories/SyncRepository.cs` — conflict entity mapping
- `ConflictItemViewModel` — `conflict.AccountId` → `conflict.Remote.AccountId.Id`
- `Infrastructure/Sync/RemoteFolderEnumerator.cs` — conflict construction site

---

## Clump 5 — Local File Location in SyncConflict (2 consumers)

**Fields:** `LocalPath` (string), `RelativePath` (string)

**Question answered:** *Where does the conflicting file live on disk?*

| Consumer | File | Fields present |
|----------|------|----------------|
| `SyncConflict` | `Domain/SyncConflict.cs:15-16` | Both — as raw strings |
| `SyncFileTarget` | `Domain/SyncFileTarget.cs` | Extracted record — used by `SyncJob` |

**Pattern:** Identical to Clump 4. `SyncConflict` carries the same local-path pair as `SyncJob.Target` but as raw strings, creating an inconsistency between the two domain objects that represent the same operation from different perspectives.

**Proposed change:** Replace `LocalPath` + `RelativePath` with `SyncFileTarget Target { get; init; }`.

**Migration impact:**
- `Domain/SyncConflict.cs:15-16` — remove `LocalPath`, `RelativePath`; add `SyncFileTarget Target`
- `ConflictItemViewModel` — `conflict.RelativePath` → `conflict.Target.RelativePath`, `conflict.LocalPath` → `conflict.Target.LocalPath`
- `Data/Repositories/SyncRepository.cs` — conflict entity mapping

---

## Clump 6 — Conflict Snapshot (2 consumers)

**Fields:** `LocalModified` (DateTimeOffset), `LocalSize` (long), `RemoteModified` (DateTimeOffset), `RemoteSize` (long)

**Question answered:** *What were the local and remote file states when the conflict was detected?*

| Consumer | File | Fields present |
|----------|------|----------------|
| `SyncConflict` | `Domain/SyncConflict.cs:18-21` | All four |
| `ConflictItemViewModel` | `Conflicts/ConflictItemViewModel.cs:18-21` | Re-exposes all four; derives four formatted text properties |

**Pattern:** These four values are detected, persisted, and displayed as a unit. `ConflictResolver` reads `LocalModified` and `RemoteModified` together to apply `LastWriteWins` policy. `ConflictItemViewModel` projects all four into display text simultaneously.

**Proposed extraction:**

```csharp
// Domain/ConflictSnapshot.cs
public sealed record ConflictSnapshot(DateTimeOffset LocalModified, long LocalSize, DateTimeOffset RemoteModified, long RemoteSize);

// Domain/ConflictSnapshotFactory.cs
public static class ConflictSnapshotFactory
{
    public static ConflictSnapshot Create(DateTimeOffset localModified, long localSize, DateTimeOffset remoteModified, long remoteSize)
        => new(localModified, localSize, remoteModified, remoteSize);
}
```

**Migration impact:**
- `Domain/SyncConflict.cs` — replace four flat fields with `ConflictSnapshot Snapshot { get; init; }`
- `Conflicts/ConflictResolver.cs` — `conflict.LocalModified` → `conflict.Snapshot.LocalModified` etc.
- `ConflictItemViewModel` — all four properties delegate to `conflict.Snapshot.*`

---

## Clump 7 — Version Tags in SyncedItemEntity (2 consumers)

**Fields:** `ETag` (string?), `CTag` (string?)

**Question answered:** *What Graph API change-detection tags identify this version of the item?*

| Consumer | File | Fields present |
|----------|------|----------------|
| `SyncedItemEntity` | `Data/Entities/SyncedItemEntity.cs:16-17` | Both — flat nullable strings |
| `VersionInfo` | `Domain/VersionInfo.cs` | Extracted record — used by `DeltaItem` |

**Pattern:** `DeltaItem` already uses `VersionInfo` for ETag/CTag. `SyncedItemEntity` persists the same data as flat columns instead of using an owned type. When ETag/CTag are read from the entity and compared against a `DeltaItem.VersionInfo`, the mismatch forces manual field mapping at every comparison site.

**Proposed change:** Apply EF Core Owned Entity Type so `SyncedItemEntity` holds a `VersionInfo Tags` property backed by the same columns.

```csharp
// SyncedItemEntity.cs — replace ETag + CTag with:
public VersionInfo Tags { get; set; } = new(null, null);

// AppDbContext / entity configuration:
entity.OwnsOne(e => e.Tags, b =>
{
    b.Property(v => v.ETag).HasColumnName("ETag");
    b.Property(v => v.CTag).HasColumnName("CTag");
});
```

**Migration impact:**
- `Data/Entities/SyncedItemEntity.cs:16-17` — remove `ETag`, `CTag`; add `VersionInfo Tags`
- `Data/Repositories/SyncedItemRepository.cs` — any `entity.ETag` → `entity.Tags.ETag`
- `Infrastructure/Sync/RemoteFolderEnumerator.cs` — ETag comparison sites
- A new EF Core migration is required

---

## Cross-Cutting Note — SyncConflict

`SyncConflict` is the primary clump aggregator. It contains **four** separate data-clumps (Clumps 4, 5, 6, and the implicit identity/state fields). After applying all four extractions, it becomes:

```csharp
public sealed class SyncConflict
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public RemoteItemRef Remote { get; init; }
    public SyncFileTarget Target { get; init; }
    public ConflictSnapshot Snapshot { get; init; }
    public ConflictState State { get; set; } = ConflictState.Pending;
    public ConflictPolicy? Resolution { get; set; }
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}
```

Field count: **16 → 8**. Every consumer site gains named access (`conflict.Remote.AccountId`, `conflict.Snapshot.LocalModified`) matching the pattern already established by `SyncJob`.

---

## Recommended Implementation Order

1. **Clump 6** — `ConflictSnapshot` — no existing record, new domain type, touches only `SyncConflict` and `ConflictItemViewModel`
2. **Clumps 4 + 5** — apply `RemoteItemRef` and `SyncFileTarget` to `SyncConflict` — types already exist, alignment refactor only
3. **Clump 7** — EF Core Owned Type for `VersionInfo` — requires migration; isolate in a single PR
4. **Clump 3** — `StorageQuota` — small record, touches two entities and one ViewModel
5. **Clump 2** — `AccountSyncConfig` — medium impact; `AccountSettings` can be deleted afterward
6. **Clump 1** — `AccountProfile` — highest consumer count; save for last as it touches all account-facing layers
