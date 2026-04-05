# S013 — Activity View

**Phase:** MVP  
**Area:** Features/Activity  
**Spec refs:** Section 7 (Activity nav item)

---

## User Story

As a user,  
I want a live feed of the last 50 sync activity items (newest first) so I can quickly see what the app has been doing without opening the full log viewer.

---

## Acceptance Criteria

- [x] Activity view displays the last 50 items, newest first
- [x] Each activity item shows: timestamp (formatted per LO-07), account name, action type (Downloaded, Uploaded, Skipped, Conflict Detected, Error), file name/path
- [x] List updates in real time during an active sync — new items prepended; list truncated to 50 as items are added
- [x] No filter controls in MVP — all accounts shown together (account column differentiates entries)
- [x] Empty state: "No sync activity yet — start a sync to see activity here"
- [x] Activity feed is **ephemeral** — stored in memory only; cleared on app restart (not persisted to SQLite)
- [x] `IActivityFeedService` (singleton) publishes `IObservable<ActivityItem>` that `ActivityViewModel` subscribes to
- [x] Sync engine (S010) publishes to `IActivityFeedService` after each file operation

### Tests
- [x] **Unit test**: `ActivityFeedService` — adding 51 items trims to 50; newest item is first
- [x] **Unit test**: `ActivityViewModel` — subscription to feed updates the `Items` observable collection
- [x] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `ActivityViewModel` is **scoped** (recreated on each navigation); subscribes to `IActivityFeedService` on activation
- `ObservableCollection<ActivityItem>` updated on UI thread via `ObserveOn(RxApp.MainThreadScheduler)`
- NF-02: list updates must not cause UI stutter — batch UI updates if > 5 items arrive per second
- NF-16: `IActivityFeedService` methods return `Option<IReadOnlyList<ActivityItem>>`

---

## Implementation Constraints

- `ObserveOn(RxApp.MainThreadScheduler)` is **required** on every observable chain that feeds `ObservableCollection<ActivityItem>`. Mutating an `ObservableCollection<T>` from a background thread throws immediately at runtime with no compile-time warning.
- Prepending to `ObservableCollection<T>` via `Insert(0, item)` causes an O(n) index shift on every insert. For the 50-item cap this is acceptable; do not increase the cap without re-evaluating this approach.
- Batch UI updates when items arrive at high frequency: buffer with `Observable.Buffer(TimeSpan.FromMilliseconds(200))` and apply the batch in one `foreach`; never call `Insert` per item from a high-frequency stream (NF-02).
- `ActivityViewModel` is scoped — a new subscription is created on each navigation. Dispose the subscription in `Dispose()` or `OnNavigatedFrom()`; failing to do so causes `IActivityFeedService` to hold a reference to the previous dead VM and duplicate items to arrive on the next navigation.
- Register the Activity nav item in `ShellServiceExtensions` only when this story ships (NF-15); until then the nav item remains disabled.

---

## Dependencies

- S001 (project scaffolding)
- S003 (navigation shell — Activity nav item)
- S005 (localisation — timestamp formatting)
- S010 (sync engine — publishes activity events)
