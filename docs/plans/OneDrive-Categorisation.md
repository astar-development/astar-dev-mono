# OneDrive File Categorisation — Approach Proposals

## Existing System

`FileClassification(Level1, Level2, Level3, IsSpecial)` already exists. `FileClassifier.Classify()` tokenises a remote path and matches user-defined `FileClassificationRule` (keyword → classification) records.

**This proposal is separate**: auto-derive Level1/2/3 values from descriptive natural-language filenames rather than requiring the user to pre-define keyword rules.

The derived categories feed the planned UI: filter files by Level1 alone, Level2 alone, or all three levels combined.

## Problem

Current auto-classification in `AStar.Dev.OneDrive.Client.Sync` requires manually authored keyword rules.
We want to extract meaningful Level1/Level2/Level3 values from **full file paths** (folder hierarchy + filename combined) without manual rules.

### Examples

| Full path / filename                                        | Level1   | Level2       | Level3      |
| ----------------------------------------------------------- | -------- | ------------ | ----------- |
| `Photos/a red car on the road.jpg`                          | `Color`  | `Red`        | `Red Car`   |
| `Photos/a red dress on the floor.jpg`                       | `Color`  | `Red`        | `Red Dress` |
| `Misc/a file with red in it's name.jpg`                     | `Color`  | `Red`        | _(none)_    |
| `People/a file with a persons name: john smith - in it.jpg` | `Person` | `John Smith` | _(none)_    |

`TagName` (existing property) returns most-specific level — so a UI search on Level1="Color" matches all colour files; Level3="Red Car" narrows to compound matches.

---

## Approach A — Rule-Based: Stop-Word Removal + Noun-Phrase Heuristics

### How it works

1. Split full path into tokens (folder names + filename stem, strip extension).
2. Remove English stop words (`a`, `an`, `the`, `on`, `in`, `of`, `with`, `at`, …) via a `static readonly HashSet<string>` (~150 words, no package needed).
3. Detect Title Case sequences (`\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)+\b`) as person/place names → emit as-is.
4. From remaining tokens, extract the first adjective+noun pair (small hardcoded colour/adjective list + simple noun filter) as the category.
5. Fallback: first remaining token after stop-word removal.

### Walkthrough on examples

```
"a red car on the road.jpg"
  tokens after stop-words: [red, car, road]
  adj+noun match: "red car" ✓

"a red dress on the floor.jpg"
  tokens after stop-words: [red, dress, floor]
  adj+noun match: "red dress" ✓

"a file with red in it's name.jpg"
  tokens after stop-words: [file, red, name]
  no adj+noun pair → first meaningful token: "red" ✓

"a file with a persons name: john smith - in it.jpg"
  Title Case regex: "John Smith" → emit immediately ✓
```

Folder context participates in token stream, e.g. `Wedding Photos/2023/Paris/bride.jpg` contributes `Wedding`, `Paris`, `bride`.

### Pros

- Zero external dependencies; pure C# (~150 LoC total)
- <1 ms per file; fully offline
- Deterministic — same input always same output
- Extends existing `FileClassificationFactory` pattern; fits current conventions

### Cons

- ~85–90% accuracy on natural-language filenames; fails on unusual phrasing
- Adjective/noun detection relies on hardcoded word lists — needs maintenance
- No semantic understanding (can't distinguish `"Apple" = fruit` vs `"Apple" = brand`)

---

## Approach B — NLP/NER Library: OpenNLP.NET

### How it works

1. Load OpenNLP English tokenizer + POS tagger + chunker models at startup (~15 MB download, offline after).
2. Feed the path/filename string into the pipeline.
3. Extract noun phrases (NP chunks) and named entities (Person, Location, Org).
4. Rank by position — leftmost non-generic NP wins.

### Pros

- ~90–92% NER accuracy on Person/Location (vs ~80% for heuristics alone)
- Handles ambiguous phrasing the rule engine would miss
- Offline after first model download

### Cons

- 15 MB model files per language; ~50–100 MB RAM loaded
- Cold-start latency (model load) — bad for per-file calls; needs warm cache
- OpenNLP.NET is community-maintained, not officially supported on .NET 10 (may need porting workaround)
- ~30–50 LoC integration but significant infrastructure (model download, lifecycle management)
- Deterministic but opaque — hard to debug wrong outputs

---

## Approach C — LLM API: Claude Haiku (Batched)

### How it works

1. Collect paths to categorise, batch 10 at a time.
2. Call Claude Haiku with a cached system prompt defining the task + examples.
3. System prompt is ephemeral-cached — only the first batch pays full input cost; subsequent batches pay ~10% of input tokens.
4. Haiku returns `[{ "filename": "...", "category": "..." }]` JSON per batch.
5. Parse + persist. Handle API errors via `Result<T,E>`.

### Estimated cost at scale (300k–500k files)

| Files   | Cost (Haiku) | Batches (10/batch) | Wall-clock (100 ms/batch, parallelised ×10) |
| ------- | ------------ | ------------------ | ------------------------------------------- |
| 300,000 | ~$33         | 30,000             | ~5 min                                      |
| 500,000 | ~$55         | 50,000             | ~8 min                                      |

Cost applies **per full run**. Re-categorisation (e.g. after a model upgrade or rule change) doubles/triples total spend.
Requires network + API key; not offline.

### Pros

- ~97–99% semantic accuracy — understands context, ambiguity, cultural references
- No maintenance of word lists or models
- Folder hierarchy used as semantic context with zero extra code
- Handles edge cases rule-based never could (`"a woman in a little black dress.jpg"` → `"black dress"`)

### Cons

- Network dependency — fails without internet / API key
- Latency: batched 10 files ~100 ms; sequential is ~10× slower
- Cost: small but non-zero; scales with file count
- Non-deterministic — rare but possible category variation across runs
- Requires `Anthropic` SDK integration (no existing wrapper in repo)

---

## Comparison Summary

|                   | A: Rule-Based  | B: OpenNLP.NET               | C: Claude Haiku         |
| ----------------- | -------------- | ---------------------------- | ----------------------- |
| Accuracy          | 85–90%         | 90–92%                       | 97–99%                  |
| Setup effort      | Low (~150 LoC) | Medium (model DL, lifecycle) | Medium (SDK + service)  |
| Dependencies      | None           | OpenNLP.NET + models         | Anthropic SDK + API key |
| Offline           | Yes            | Yes (after download)         | No                      |
| Latency per file  | <1 ms          | ~5 ms (warm)                 | ~10 ms (batched)        |
| Cost (500k files) | Free           | Free                         | ~$55/run                |
| Deterministic     | Yes            | Yes                          | No                      |
| Re-run cost       | Free           | Free                         | $55 each time           |

---

## Answered Questions

**Q1 — Scale:** 300k–500k files.
Impact: Approach C costs $33–55 **per run**. Re-categorisation (model upgrade, rule change, bug fix) multiplies that. At this scale, free + offline wins unless accuracy becomes a hard requirement.

**Q2 — Category usage:** No current UI; intent is a search/filter UI by Level1, Level2, or Level3.
Impact: Categories are stored once and queried repeatedly. Stability matters more than perfection — a misfiled "red" vs "red car" is annoying in UI search but not catastrophic. 85–90% accuracy is acceptable for v1.

**Q3 — Determinism ("eventual consistency"):**
Poor phrasing on my part — retracted. The real question was: _if you re-run categorisation, will the same file always get the same category?_

- Approaches A and B: yes, always identical (deterministic). No concern.
- Approach C (LLM): rarely but possibly different across runs (model temperature, API version drift). If a user searches for "Red Car" and half the matching files were later re-categorised as "Car", search results would be silently incomplete.

At 300k–500k files, re-running Approach C is expensive anyway, so in practice categories would be written once and never re-derived. The non-determinism risk is therefore low — but it's a gotcha to be aware of before committing to C.

---

## Recommendation

**Approach A — Rule-Based.** Scale kills Approach C as a default choice.

### Rationale

- 500k files × $55/run is non-trivial, especially if re-categorisation is ever needed.
- 83 minutes wall-clock (even parallelised to ~8 min) adds sync pipeline complexity for a first pass.
- 85–90% accuracy is fine for a search UI where the user can browse by Level1 to find what they need.
- Approach B's .NET 10 maturity risk is not worth 2–3% accuracy gain; skip it.
- Approach C remains a valid future option for a **user-triggered "re-analyse selected folder"** flow (small scope, user pays knowingly).

### Implementation phases

Constraints: ≤6 files per phase (including test files). App stays operational throughout — new files are unwired until Phase 5.
Auto-categorisation runs for **all** files. Results persist to the existing `SyncedItemClassificationEntity` (same as keyword-rule classifications).

---

#### Phase 1 — Path normalisation (2 files)

| File                                   | Status |
| -------------------------------------- | ------ |
| `Domain/PathNormaliser.cs`             | New    |
| `Domain.Tests/GivenAPathNormaliser.cs` | New    |

Responsibility: strip the 7 root segments (`RootSegmentsToSkip = 7`), then split remaining path into folder segments and filename stem.
Not wired to anything. App unaffected.

---

#### Phase 2 — Token analysis (2 files)

| File                                  | Status |
| ------------------------------------- | ------ |
| `Domain/TokenAnalyser.cs`             | New    |
| `Domain.Tests/GivenATokenAnalyser.cs` | New    |

Responsibility:

- `static readonly HashSet<string> StopWords` (~150 English stop words)
- `static readonly HashSet<string> ColourWords` (red, blue, green, black, white, …)
- `ExtractPersonName(string text)` — Title Case regex → `Option<string>`
- `ExtractColourPhrase(IReadOnlyList<string> tokens)` — first colour + immediately-following noun → `Option<string>`

Not wired. App unaffected.

---

#### Phase 3 — Level1 derivation (2 files)

| File                                  | Status |
| ------------------------------------- | ------ |
| `Domain/Level1Deriver.cs`             | New    |
| `Domain.Tests/GivenALevel1Deriver.cs` | New    |

Responsibility:

- `static readonly Dictionary<string, string> FolderTypeMap` (`"people" → "Person"`, `"places" → "Place"`, `"events" → "Event"`, `"photos" → "Unclassified"`, …)
- `Derive(IReadOnlyList<string> folderSegments, IReadOnlyList<string> filenameTokens)` → `string` (Level1 value)
- Priority: folder match first; inferred fallback (person name detected → `"Person"`, colour detected → `"Color"`, else → `"Unclassified"`)

Not wired. App unaffected.

---

#### Phase 4 — Auto-categorisor assembly (3 files)

| File                                                 | Status |
| ---------------------------------------------------- | ------ |
| `Domain/IFileAutoCategorisor.cs`                     | New    |
| `Domain/RuleBasedFileAutoCategorisor.cs`             | New    |
| `Domain.Tests/GivenARuleBasedFileAutoCategorisor.cs` | New    |

`IFileAutoCategorisor`:

```csharp
public interface IFileAutoCategorisor
{
    FileClassification Categorise(string remotePath);
}
```

`RuleBasedFileAutoCategorisor` assembles Phases 1–3 into a `FileClassification(Level1, Level2, Level3, isSpecial: false)`:

- Level1 — from `Level1Deriver`
- Level2 — specific value (person name or colour word)
- Level3 — compound phrase (adj+noun) if different from Level2, else `Option.None`

End-to-end tests covering all four example filenames plus edge cases (empty path, only root segments, no meaningful tokens).
Registered in DI but **not injected anywhere yet**. App unaffected.

---

#### Phase 5 — Wire up (2 files)

| File                                              | Status   |
| ------------------------------------------------- | -------- |
| `Infrastructure/Sync/Jobs/SyncedItemRegistrar.cs` | Modified |
| _(service registration file)_                     | Modified |

`SyncedItemRegistrar` already persists keyword-rule `FileClassification` results. Inject `IFileAutoCategorisor`, call `Categorise(remotePath)`, and append the result to the same `SyncedItemClassificationEntity` collection.
No DB migration required — existing table accepts additional rows per file.

**Future:** add `ClaudeHaikuFileAutoCategorisor` (Approach C) as an alternate `IFileAutoCategorisor` behind a user-facing "smart re-analyse selected folder" command.

### Level1 strategy — Combined Derived + Inferred

Priority chain (~20 extra lines on top of Approach A):

1. **Derived:** map the nearest meaningful folder name against a small dictionary (`"People" → "Person"`, `"Places" → "Place"`, `"Events" → "Event"`, `"Photos" → "Unclassified"`, …). If match found → Level1 = mapped value.
2. **Inferred fallback:** if no folder match, inspect filename tokens — Title Case sequence detected → `"Person"`; colour word detected → `"Color"`; else → `"Unclassified"`.

**Conflict rule:** folder wins for Level1 (represents deliberate user organisation). Level2/Level3 always come from filename content regardless.

Example: `People/a red car.jpg` → Level1 = `Person` (folder), Level2 = `Red` (colour token), Level3 = `Red Car` (adj+noun).

---

### Path prefix exclusion

Several leading path segments add noise — the root sync path (`/Users/jason/OneDrive/`, or whatever is configured) contributes tokens like `Users`, `OneDrive`, `jason` that should never influence classification.

**Approach: strip a hard-coded prefix segment count before tokenising.**

```csharp
// Hard-coded for now — 8 '/' in the root sync path = 7 meaningful segments to skip
private const int RootSegmentsToSkip = 7;

private static string StripRootPath(string remotePath)
{
    var segments = remotePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

    return segments.Length <= RootSegmentsToSkip
        ? string.Empty
        : string.Join('/', segments.Skip(RootSegmentsToSkip));
}
```

`StripRootPath` runs before `Tokenise`. The result is passed into the Level1 derived-folder lookup and the Level2/Level3 noun-phrase extraction.

**Future:** replace `RootSegmentsToSkip` with the configured sync root path from app settings — strip the prefix string directly rather than counting segments.
