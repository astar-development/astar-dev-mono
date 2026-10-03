# How the scraper hangs together

This document explains how a scrape runs, from the **Run** button to a wallpaper on disk and in the database. All paths are relative to `AStarDev.WallpaperScraper/` unless stated.

## 1. The big picture

The app is an Avalonia desktop UI over a pipeline that talks to the Wallhaven API, downloads images, and records them in a SQLite database (EF Core, `packages/AStarDev.ControlDb`).

```mermaid
flowchart LR
    UI["Avalonia UI<br/>MainWindow"] --> Svc["Scraping<br/>ScrapeService → SearchOrchestrator → PagesProcessor"]
    Svc --> Ing["WallpaperIngestion<br/>download, save, record"]
    Svc --> API[("Wallhaven API<br/>search + detail JSON")]
    Ing --> API
    Ing --> CDN[("Wallhaven image host")]
    Ing --> Disk[/"Disk<br/>saved images"/]
    Svc --> DB[("SQLite<br/>ControlDb")]
    Ing --> DB
    Ing -. "ImageDownloadNotifier event" .-> UI
```

Folders are grouped by domain, not by type:

| Folder                 | Responsibility                                                               |
| ---------------------- | ---------------------------------------------------------------------------- |
| `UI/`                  | Windows, button handlers, status log, image preview                          |
| `Operations/`          | `OperationCoordinator` - one operation at a time, plus cancellation          |
| `Scoping/`             | `ScopedRunner` - runs work inside a DI scope                                 |
| `Scraping/`            | Paging through search results, resume logic, API client, rate limiting, tags |
| `WallpaperIngestion/`  | Deciding what is new, downloading, saving, recording, announcing             |
| `ScrapeConfiguration/` | Loading, editing, importing and exporting the configuration                  |
| `Startup/`             | DI registration and database initialisation                                  |

## 2. The call chain

Each layer does one job and hands down to the next.

```mermaid
flowchart TD
    A["MainWindow.RunScraper<br/>builds ScrapeSelection from the 3 switches"] --> B["ScrapeActions.RunScraperAsync"]
    B --> C["ScrapeRunner.RunAsync<br/>Task.Run, reports failures to status"]
    C --> D["ScrapeService.RunScraperAsync<br/>single active operation, timing, one failure report"]
    D --> E["ScopedRunner.RunAsync&lt;IUnitOfWork, ISearchOrchestrator&gt;<br/>new DI scope per scrape"]
    E --> F["SearchOrchestrator.RunSearchesAsync<br/>hot, top, then each category"]
    F --> G["PagesProcessor.FetchAndProcessPagesAsync<br/>one search, page by page"]
    G --> H["PageIngestionStep.IngestPageAsync<br/>one page, then SaveChanges"]
    H --> I["WallpaperIngestionService.IngestPageAsync<br/>split new vs existing"]
    I --> J["NewWallpaperIngestor<br/>FetchTagsAsync / IngestAsync"]
    J --> K["WallpaperFiler.FileAsync<br/>save, link tags, announce"]
    K --> L["WallpaperSaver.SaveAsync"]
    L --> M["ImageDownloader.DownloadAsync"]
    L --> N["WallpaperFileRecorder.Record"]
    K --> O["TagLinker.LinkTagsAsync"]
    K --> P["ImageDownloadNotifier.NotifyImageDownloaded"]
```

### What each class does

| Class                                          | Method                                                              | Job                                                                                                                                                                  |
| ---------------------------------------------- | ------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `UI/MainWindow`                                | `RunScraper`                                                        | Reads the hot, top and categories switches into a `ScrapeSelection`                                                                                                  |
| `UI/ScrapeActions`                             | `RunScraperAsync`, `CancelOperation`                                | Button actions; cancel calls `OperationCoordinator.Cancel`                                                                                                           |
| `UI/ScrapeRunner`                              | `RunAsync`                                                          | Runs the scrape on the thread pool, so continuations never resume on the UI thread; catches anything that escapes                                                    |
| `Scraping/ScrapeService`                       | `RunScraperAsync`                                                   | Takes the single-operation lock via `OperationCoordinator.TryStart`, times the run, reports completion, cancellation or failure once                                 |
| `Scoping/ScopedRunner`                         | `RunAsync`                                                          | Creates a DI scope so scoped services (DbContext, tag linker) live for exactly one scrape                                                                            |
| `Scraping/SearchOrchestrator`                  | `RunSearchesAsync`                                                  | Builds a `PageScrapeRequest` for hot, top, and each included category (honouring `ScrapeSelection` and `ScrapeLimits`); a `PageFetchException` ends only that search |
| `Scraping/PagesProcessor`                      | `FetchAndProcessPagesAsync`                                         | Resolves the start page, applies the resume policy, loops over pages with a pacing delay                                                                             |
| `Scraping/ScrapeResumePolicy`                  | `ResumePage`, `IsUnchangedSincePreviousScrape`, `IsLastPageToVisit` | Pure decisions: where to resume, whether to skip a finished category, when to stop                                                                                   |
| `Scraping/PageIngestionStep`                   | `IngestPageAsync`                                                   | Ingests one page, records category progress, calls `SaveChangesAsync`; on cancel it saves what was downloaded so far                                                 |
| `WallpaperIngestion/WallpaperIngestionService` | `IngestPageAsync`                                                   | Asks the DB which wallpapers already exist, skips those, pipelines tag fetching for the rest                                                                         |
| `WallpaperIngestion/NewWallpaperIngestor`      | `FetchTagsAsync`, `IngestAsync`                                     | Fetches tags; wallpapers with an "ignore" tag are remembered and not downloaded                                                                                      |
| `WallpaperIngestion/WallpaperFiler`            | `FileAsync`                                                         | Chooses directory and file name from tags, saves, links tags, announces                                                                                              |
| `WallpaperIngestion/WallpaperSaver`            | `SaveAsync`                                                         | Downloads the image, then records a `FileEntity`                                                                                                                     |
| `WallpaperIngestion/ImageDownloader`           | `DownloadAsync`                                                     | Paced download to a `.part` file, then moved into place                                                                                                              |
| `Scraping/TagLinker`                           | `LinkTagsAsync`                                                     | Creates or reuses `TagEntity` rows and links them to the file                                                                                                        |

## 3. End-to-end sequence

```mermaid
sequenceDiagram
    actor User
    participant UI as MainWindow / ScrapeRunner
    participant Svc as ScrapeService
    participant Orc as SearchOrchestrator
    participant Pg as PagesProcessor
    participant Step as PageIngestionStep
    participant Ing as WallpaperIngestionService
    participant API as Wallhaven API
    participant DB as SQLite
    participant Img as Image host

    User->>UI: click Run
    UI->>Svc: RunScraperAsync(selection, progress)
    Svc->>Svc: OperationCoordinator.TryStart (else "already running")
    Svc->>DB: load ScrapeConfiguration (inside a new scope)
    Svc->>Orc: RunSearchesAsync(configuration, selection)
    loop each selected search (hot, top, categories)
        Orc->>Pg: FetchAndProcessPagesAsync(request)
        Pg->>API: fetch resume page
        Pg->>Pg: ResumePolicy: skip category if unchanged
        loop each page
            Pg->>Step: IngestPageAsync(page)
            Step->>Ing: IngestPageAsync(wallpapers)
            Ing->>DB: which handles already exist?
            loop each new wallpaper
                Ing->>API: GET detail (tags)
                Ing->>Img: GET image (after pacing delay)
                Ing->>DB: add FileEntity + tag links (tracked)
            end
            Step->>DB: SaveChanges (once per page)
            Pg->>Pg: pacing delay
            Pg->>API: fetch next page
        end
    end
    Svc-->>UI: "Search completed in ..."
```

## 4. Page loop and resume logic

A category is only ever marked as progressed when a page was **fully** ingested. If any wallpaper on a page fails, progress stops moving, so the next scrape retries from that page.

```mermaid
flowchart TD
    S["Start search"] --> R["resumePage = ResumePolicy.ResumePage(previous progress)"]
    R --> F1["Fetch resumePage"]
    F1 --> Same{"resumePage > 1 and<br/>same category as last time?<br/>(total + last page match)"}
    Same -- "no" --> F2["Fetch page 1 instead"]
    Same -- "yes" --> U
    F2 --> U{"Unchanged and fully<br/>scraped last time?"}
    U -- "yes" --> Skip["Report 'nothing has changed' and stop"]
    U -- "no" --> Tally["Tally.RecordSkipped for pages resumed past"]
    Tally --> Ing["IngestPageAsync(page)"]
    Ing --> Out{"Outcome"}
    Out -- "Complete and progress not withheld" --> Rec["OnPageCompleted: record last page and total on the category"]
    Out -- "Incomplete" --> With["Withhold progress from here on"]
    Rec --> Last
    With --> Last
    Ing --> Save["SaveChanges"]
    Last{"IsLastPageToVisit?<br/>page >= LastPage or MaximumPagesPerSearch"}
    Last -- "yes" --> Done["End search"]
    Last -- "no" --> Wait["Task.Delay(DownloadPacing) - avoids HTTP 429"]
    Wait --> Next["Fetch next page"] --> Ing
```

Hot and top have no stored progress (`Option.None`), so they always start at page 1.

## 5. Ingesting one page

`WallpaperIngestionService` pipelines the work: while one wallpaper downloads and is recorded, the next wallpaper's tags are already being fetched. The two are never parallel with themselves, so the API limit and the single, non-thread-safe `DbContext` are both respected.

```mermaid
flowchart TD
    P["Page of wallpapers (Data[])"] --> Q["FilesQuery.GetExistingHandlesAsync"]
    Q -- "query failed" --> Inc1["Incomplete, reported"]
    Q --> Split["SplitNewCandidates"]
    Split -- "already in DB" --> Skip["Tally.RecordSkipped(1)"]
    Split -- "new" --> Pre["Prefetch tags for wallpaper 0"]
    Pre --> Loop["for each new wallpaper"]
    Loop --> Tags{"Tags fetched?"}
    Tags -- "no" --> Inc2["Incomplete"]
    Tags -- "yes" --> Ign{"Any tag flagged IgnoreImage?"}
    Ign -- "yes" --> Rem["IgnoredWallpaperRecorder.Remember<br/>counts as Complete"]
    Ign -- "no" --> File["WallpaperFiler.FileAsync"]
    File --> Ok{"Result"}
    Ok -- "saved and linked" --> Cmp["Complete"]
    Ok -- "link failed" --> Disc["Discard FileEntity, Incomplete"]
    Ok -- "exception" --> Inc3["Incomplete, reported, not thrown"]
```

The next wallpaper's tag fetch is started before the current one is filed (`prefetch` in `IngestNewWallpapersAsync`).

### Filing one wallpaper

```mermaid
flowchart LR
    T["tags"] --> Dir["SaveDirectories.For(tags)<br/>famous root or normal root, tag-name segment, category segment"]
    T --> Name["WallpaperFileNamer.Create(id, extension, tags)"]
    Dir --> Req["WallpaperFileRequest"]
    Name --> Req
    Req --> Dl["ImageDownloader<br/>random 2-3s delay, GET, write .part, move"]
    Dl --> Rec["WallpaperFileRecorder.Record<br/>FileEntity + access detail + image detail"]
    Rec --> Link["TagLinker.LinkTagsAsync"]
    Link -- "ok" --> Ann["RecordDownload, then NotifyImageDownloaded"]
    Link -- "fails or cancelled" --> Del["Delete FileEntity"]
```

The file is recorded **before** the tags are linked. If linking fails, the entity is discarded, because otherwise the page save would keep an untagged file that later scrapes would treat as already ingested.

## 6. Counting: the "(x of y)" in the preview

`IngestionRun` carries a mutable `ScrapeTally` for the lifetime of one search.

```mermaid
flowchart LR
    Pg["PageIngestionStep<br/>Tally.Total = Meta.Total"] --> T(("ScrapeTally<br/>Total, Current"))
    Resume["PagesProcessor<br/>RecordSkipped((startPage - 1) x perPage)"] --> T
    Exist["WallpaperIngestionService<br/>RecordSkipped(1) per existing wallpaper"] --> T
    Dl["WallpaperFiler<br/>RecordDownload()"] --> T
    T --> Info["WallpaperInfo.Count (SearchCount)"]
    Info --> Lbl["CategoryDescription: 'Top Wallpapers (1,234 of 5,678)'"]
```

`x` therefore means downloaded plus skipped, so a partially scraped category shows a sensible number. The skip count for a resumed category is an estimate (start page number minus one, times the wallpapers on that page).

## 7. The preview: from background thread to UI

Ingestion runs on a thread-pool thread, so the preview is decoupled by an event and a coalescing decoder.

```mermaid
sequenceDiagram
    participant Filer as WallpaperFiler (scrape thread)
    participant Notif as ImageDownloadNotifier (singleton)
    participant Coord as ImageDisplayCoordinator (singleton)
    participant Panel as ImagePreviewPanel (UI)

    Filer->>Notif: NotifyImageDownloaded(details)
    Notif-->>Coord: ImageDownloaded event
    alt IsEnabled and not already decoding
        Coord->>Coord: Task.Run decode to PNG (max 1280px)
    else already decoding
        Coord->>Coord: keep only the newest waiting image
    end
    Coord-->>Panel: ImageReady(WallpaperPreviewImage)
    Panel->>Panel: Dispatcher.UIThread.Post(DisplayImage)
```

If downloads outpace decoding, intermediate images are dropped and only the latest is shown. A decode failure is swallowed, because a missing preview is not worth stopping a scrape.

Progress text follows a similar path: `ScrapeRunner` hands `IProgress<string>` to `StatusReporter`, which appends to the status log.

## 8. HTTP pipeline and rate limiting

There are three separate throttles. They exist because the Wallhaven API and the image host behave differently.

```mermaid
flowchart LR
    Code["JsonResponseProcessor / ImageDownloader"] --> Client["HttpClient<br/>from WallhavenClientFactory"]
    Client --> H1["WallhavenApiKeyHandler<br/>strip API key from non-API requests"]
    H1 --> H2["WallhavenRateLimitingHandler<br/>API only: sliding window permit,<br/>retry 429 using Retry-After, up to 3 times"]
    H2 --> Net(("Network"))
```

| Throttle                                    | Where                          | Applies to                                                                                 |
| ------------------------------------------- | ------------------------------ | ------------------------------------------------------------------------------------------ |
| Sliding-window `RateLimiter` plus 429 retry | `WallhavenRateLimitingHandler` | Every request to the API path (search pages, tag detail)                                   |
| Random 2-3s `DownloadPacing`                | `ImageDownloader`              | Before each image download                                                                 |
| Same `DownloadPacing`                       | `PagesProcessor`               | Before each page after the first, so skipping already-ingested pages does not trigger 429s |

Image downloads are not rate limited by the handler, and the API key is removed from them so it is never sent to the image host.

## 9. Cancellation, single-operation and failure rules

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Running: OperationCoordinator.TryStart
    Running --> Running: TryStart refused ("already running")
    Running --> Cancelling: Cancel() on the token source
    Cancelling --> Idle: OperationCanceledException caught in ScrapeService
    Running --> Idle: Completed
    Running --> Idle: Failed, reported once with message chain
    note right of Cancelling
        PageIngestionStep saves wallpapers
        already downloaded this page
        before the cancellation propagates
    end note
```

Failure scoping, from narrowest to widest:

- A wallpaper that fails to download or link is reported, the page is marked `Incomplete`, the scrape continues.
- A page that cannot be fetched throws `PageFetchException`, which ends only that search; the next search still runs, and its progress is withheld so it resumes next time.
- Anything else, or a cancellation, propagates to `ScrapeService`, the one place a scrape failure is reported.

## 10. Dependency injection and lifetimes

`Startup/ApplicationServices.cs` composes everything. The rule: a service's lifetime must not be longer than any of its dependencies'.

```mermaid
flowchart TD
    subgraph Singleton
        SS["ScrapeService"]
        SR["ScopedRunner"]
        OC["OperationCoordinator"]
        RP["ScrapeResumePolicy"]
        RL["RateLimiter"]
        DP["DownloadPacing"]
        IN["ImageDownloadNotifier"]
        IDC["ImageDisplayCoordinator"]
        Q["IQuery implementations"]
        DF["DbContext factory"]
    end
    subgraph Scoped["Scoped (one per scrape)"]
        UOW["IUnitOfWork = ControlDbContext"]
        SO["SearchOrchestrator"]
        PP["PagesProcessor"]
        PS["PageIngestionStep"]
        WIS["WallpaperIngestionService"]
        WF["WallpaperFiler / Saver"]
        TL["TagLinker"]
        TFS["TagFlagStore"]
    end
    SS --> SR
    SR -- "creates scope" --> Scoped
    SO --> PP --> PS --> WIS --> WF --> TL
    PS --> UOW
    TL --> UOW
```

`ScrapeService` is a singleton, so it never takes scoped dependencies directly. It asks `ScopedRunner` for a fresh scope per scrape instead. The `IQuery<T>` implementations are singletons because the DbContext factory is a singleton and builds the context from the root provider, so a scoped dependency there would fail.

An integration test (`GivenTheApplicationServices`) builds the container with scope and build validation on, so a captive dependency fails the build.

## 11. Persistence

The main entities, all in `packages/AStarDev.ControlDb`:

```mermaid
erDiagram
    ScrapeConfigurationEntity ||--o{ SearchCategoryEntity : has
    FileEntity ||--|| FileAccessDetailEntity : has
    FileEntity ||--o| ImageDetailEntity : has
    FileEntity ||--o{ FileTagEntity : tagged
    TagEntity ||--o{ FileTagEntity : used
```

| Entity                        | Role in a scrape                                                                                     |
| ----------------------------- | ---------------------------------------------------------------------------------------------------- |
| `ScrapeConfigurationEntity`   | API key, base URL, hot and top settings, search string prefix and suffix, root directories           |
| `SearchCategoryEntity`        | A category; stores `LastKnownImageCount`, `LastPageVisited`, `TotalPages` for resume                 |
| `FileEntity`                  | One saved wallpaper; its `FileHandle` is the Wallhaven id, which is how "already exists" is detected |
| `TagEntity` / `FileTagEntity` | Tags and their links; flags (ignore, name, famous) decide skipping and directories                   |

`IUnitOfWork` commits once per page (`PageIngestionStep`), so a page is saved as one batch.

## 12. Where to look when...

| You want to change              | Start at                                                                       |
| ------------------------------- | ------------------------------------------------------------------------------ |
| Which scrapes run, or limits    | `SearchOrchestrator`, `ScrapeSelection`, `ScrapeLimits`                        |
| Resume / skip rules             | `ScrapeResumePolicy`, `PagesProcessor`                                         |
| Waits between requests          | `DownloadPacing`, `WallhavenRateLimitingHandler`, `ApplicationConstants`       |
| Where files are saved and named | `SaveDirectories`, `SaveDirectoryResolver`, `WallpaperFileNamer`               |
| What is recorded in the DB      | `WallpaperFileRecorder`, `TagLinker`                                           |
| What is ignored                 | `TagFlagStore`, `TagFetcher`, `IgnoredWallpaperRecorder`                       |
| The preview and counts          | `ImageDisplayCoordinator`, `ImagePreviewPanel`, `ScrapeTally`, `WallpaperInfo` |
| Service wiring                  | `Startup/ApplicationServices.cs`, `Startup/DataServices.cs`                    |
