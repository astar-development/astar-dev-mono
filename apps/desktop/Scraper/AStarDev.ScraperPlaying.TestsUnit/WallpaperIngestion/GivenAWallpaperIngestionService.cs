using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperIngestionService
{
    private static readonly string[] WallpaperIds = ["a", "b", "c", "d"];
    private readonly FakeRepository<FileEntity, FileId> fileRepository = new();
    private readonly FakeFilesQuery filesQuery = new();
    private readonly FakeNewWallpaperIngestor newWallpaperIngestor = new();
    private readonly CapturingProgress progress = new();
    private readonly WallpaperIngestionService service;

    public GivenAWallpaperIngestionService() => service = new(filesQuery, newWallpaperIngestor);

    [Fact]
    public async Task when_a_wallpaper_already_exists_then_it_is_skipped_and_not_ingested()
    {
        filesQuery.ExistsResult = true;

        await Ingest(CreateWallpaper("existing-wallpaper"));

        progress.Messages.ShouldContain("The file details already exist for wallpaper existing-wallpaper - no need to fetch again.");
        newWallpaperIngestor.Ingested.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_wallpapers_already_exist_then_they_count_as_skipped_towards_the_search_count()
    {
        filesQuery.ExistingHandles.Add(new FileHandle("existing-one"));
        filesQuery.ExistingHandles.Add(new FileHandle("existing-two"));
        using var client = new HttpClient();
        var context = new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("resolved-directory", "famous-resolved-directory", ""), "resolved-category"), client, fileRepository, []);
        var run = IngestionRuns.Create(context, progress, TestContext.Current.CancellationToken);

        await service.IngestPageAsync([CreateWallpaper("existing-one"), CreateWallpaper("existing-two"), CreateWallpaper("fresh")], run);

        run.Tally.Current.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_wallpaper_is_new_then_it_is_ingested_with_the_page_context_and_progress()
    {
        var wallpaper = CreateWallpaper("new-wallpaper", path: "https://example.test/full/new-wallpaper.jpg");
        using var client = new HttpClient();
        var context = new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("resolved-directory", "famous-resolved-directory", ""), "resolved-category"), client, fileRepository, []);

        var run = IngestionRuns.Create(context, progress, TestContext.Current.CancellationToken);

        await service.IngestPageAsync([wallpaper], run);

        newWallpaperIngestor.Ingested.ShouldBe([(wallpaper, ".jpg", run)]);
    }

    [Fact]
    public async Task when_a_page_has_several_wallpapers_then_existence_is_checked_with_a_single_query_and_only_the_new_ones_are_ingested()
    {
        var existing = CreateWallpaper("existing-wallpaper", path: "https://example.test/full/existing-wallpaper.jpg");
        var fresh = CreateWallpaper("fresh-wallpaper", path: "https://example.test/full/fresh-wallpaper.jpg");
        var another = CreateWallpaper("another-wallpaper", path: "https://example.test/full/another-wallpaper.jpg");
        filesQuery.ExistingHandles.Add(new FileHandle("existing-wallpaper"));

        await Ingest(existing, fresh, another);

        (filesQuery.ExistingHandlesQueryCount, string.Join(",", newWallpaperIngestor.Ingested.Select(call => call.Wallpaper.Id))).ShouldBe((1, "fresh-wallpaper,another-wallpaper"));
    }

    [Fact]
    public async Task when_a_wallpaper_is_looked_up_then_its_id_is_the_handle_checked_regardless_of_its_extension()
    {
        await Ingest(CreateWallpaper("png-wallpaper", path: "https://example.test/full/png-wallpaper.png"), CreateWallpaper("extensionless-wallpaper", path: "https://example.test/full/extensionless-wallpaper"));

        string.Join(",", filesQuery.CheckedHandles.Select(handle => handle.Value)).ShouldBe("png-wallpaper,extensionless-wallpaper");
    }

    [Fact]
    public async Task when_a_wallpaper_is_new_then_the_real_extension_or_the_jpg_fallback_is_passed_on_for_the_file_name()
    {
        await Ingest(CreateWallpaper("png-wallpaper", path: "https://example.test/full/png-wallpaper.png"), CreateWallpaper("extensionless-wallpaper", path: "https://example.test/full/extensionless-wallpaper"));

        string.Join(",", newWallpaperIngestor.Ingested.Select(call => call.Extension)).ShouldBe(".png,.jpg");
    }

    [Fact]
    public async Task when_a_stored_file_has_a_person_prefixed_name_then_the_wallpaper_is_still_recognised_by_its_handle_and_not_ingested_again()
    {
        filesQuery.ExistingHandles.Add(new FileHandle("vqyxgm"));

        await Ingest(CreateWallpaper("vqyxgm", path: "https://example.test/full/vqyxgm.jpg"));

        (newWallpaperIngestor.Ingested.Count, progress.Messages.Single()).ShouldBe((0, "The file details already exist for wallpaper vqyxgm - no need to fetch again."));
    }

    [Fact]
    public async Task when_the_stored_name_differs_only_by_case_then_the_wallpaper_is_still_treated_as_existing()
    {
        filesQuery.ExistingHandles.Add(new FileHandle("mixed-case"));

        await Ingest(CreateWallpaper("Mixed-Case", path: "https://example.test/full/Mixed-Case.jpg"));

        newWallpaperIngestor.Ingested.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_page_has_no_wallpapers_then_no_query_is_made()
    {
        await Ingest();

        filesQuery.ExistingHandlesQueryCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_checking_whether_wallpapers_already_exist_fails_then_the_failure_is_reported_and_the_page_is_skipped()
    {
        filesQuery.ExistsResult = new InvalidOperationException("query failed");

        await Ingest(CreateWallpaper("check-fails"));

        progress.Messages.ShouldContain("Failed to check whether the file details already exist for this page of wallpapers: query failed");
        newWallpaperIngestor.Ingested.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_wallpaper_is_being_ingested_then_the_next_wallpapers_tags_are_already_being_fetched()
    {
        var first = CreateWallpaper("first", path: "https://example.test/full/first.jpg");
        var second = CreateWallpaper("second", path: "https://example.test/full/second.jpg");
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        newWallpaperIngestor.OnIngest = (wallpaper, _) => wallpaper.Id == "first" ? releaseFirst.Task : Task.CompletedTask;

        var ingestion = Ingest(first, second);
        await WaitUntilAsync(() => newWallpaperIngestor.Fetched.Contains("second"));
        var secondFetchedWhileFirstStillIngesting = newWallpaperIngestor.Ingested.Count == 0;
        releaseFirst.SetResult();
        await ingestion;

        (secondFetchedWhileFirstStillIngesting, string.Join(",", newWallpaperIngestor.Ingested.Select(call => call.Wallpaper.Id))).ShouldBe((true, "first,second"));
    }

    [Fact]
    public async Task when_several_wallpapers_are_ingested_then_each_gets_its_own_tags_in_page_order_and_only_one_fetch_and_one_ingest_run_at_a_time()
    {
        var wallpapers = WallpaperIds.Select(id => CreateWallpaper(id, path: $"https://example.test/full/{id}.jpg")).ToArray();
        newWallpaperIngestor.OnFetch = async (_, _) => await Task.Delay(5, TestContext.Current.CancellationToken);
        newWallpaperIngestor.OnIngest = async (_, _) => await Task.Delay(5, TestContext.Current.CancellationToken);

        await Ingest(wallpapers);

        (string.Join(",", newWallpaperIngestor.Ingested.Select(call => call.Wallpaper.Id)), newWallpaperIngestor.TagsIngested.All(pair => pair.Value.Single().Name == pair.Key), newWallpaperIngestor.MaximumConcurrentFetches, newWallpaperIngestor.MaximumConcurrentIngests).ShouldBe(("a,b,c,d", true, 1, 1));
    }

    [Fact]
    public async Task when_some_wallpapers_already_exist_then_only_the_new_ones_have_their_tags_fetched()
    {
        filesQuery.ExistingHandles.Add(new FileHandle("existing"));

        await Ingest(CreateWallpaper("existing"), CreateWallpaper("fresh"));

        string.Join(",", newWallpaperIngestor.Fetched).ShouldBe("fresh");
    }

    [Fact]
    public async Task when_a_wallpapers_tags_cannot_be_fetched_then_it_is_not_ingested_but_the_rest_of_the_page_still_is()
    {
        newWallpaperIngestor.FailedFetches.Add("b");

        await Ingest(CreateWallpaper("a"), CreateWallpaper("b"), CreateWallpaper("c"));

        string.Join(",", newWallpaperIngestor.Ingested.Select(call => call.Wallpaper.Id)).ShouldBe("a,c");
    }

    [Fact]
    public async Task when_every_wallpaper_on_the_page_is_ingested_or_already_exists_then_the_page_is_complete()
    {
        filesQuery.ExistingHandles.Add(new FileHandle("existing"));

        (await Ingest(CreateWallpaper("existing"), CreateWallpaper("fresh"))).ShouldBe(IngestOutcome.Complete);
    }

    [Fact]
    public async Task when_a_page_has_no_wallpapers_then_it_is_complete()
        => (await Ingest()).ShouldBe(IngestOutcome.Complete);

    [Fact]
    public async Task when_a_wallpapers_tags_cannot_be_fetched_then_the_page_is_incomplete()
    {
        newWallpaperIngestor.FailedFetches.Add("b");

        (await Ingest(CreateWallpaper("a"), CreateWallpaper("b"), CreateWallpaper("c"))).ShouldBe(IngestOutcome.Incomplete);
    }

    [Fact]
    public async Task when_a_wallpaper_fails_to_ingest_then_the_page_is_incomplete_but_the_rest_of_the_page_is_still_ingested()
    {
        newWallpaperIngestor.FailedIngests.Add("a");

        var outcome = await Ingest(CreateWallpaper("a"), CreateWallpaper("b"));

        (outcome, string.Join(",", newWallpaperIngestor.Ingested.Select(call => call.Wallpaper.Id))).ShouldBe((IngestOutcome.Incomplete, "a,b"));
    }

    [Fact]
    public async Task when_checking_whether_wallpapers_exist_fails_then_the_page_is_incomplete()
    {
        filesQuery.ExistsResult = new InvalidOperationException("query failed");

        (await Ingest(CreateWallpaper("check-fails"))).ShouldBe(IngestOutcome.Incomplete);
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_while_a_wallpaper_is_ingesting_then_the_cancellation_propagates_and_the_prefetch_is_not_left_running()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var prefetchEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        newWallpaperIngestor.OnFetch = async (wallpaper, token) =>
        {
            if (wallpaper.Id != "second") return;

            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            finally
            {
                _ = prefetchEnded.TrySetResult();
            }
        };
        newWallpaperIngestor.OnIngest = async (_, token) =>
        {
            await WaitUntilAsync(() => newWallpaperIngestor.Fetched.Contains("second"));
            await cancellationTokenSource.CancelAsync();
            token.ThrowIfCancellationRequested();
        };
        using var client = new HttpClient();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => service.IngestPageAsync([CreateWallpaper("first"), CreateWallpaper("second")], IngestionRuns.Create(new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("some-directory", "famous-some-directory", ""), "resolved-category"), client, fileRepository, []), progress, cancellationTokenSource.Token)));

        (await Task.WhenAny(prefetchEnded.Task, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))).ShouldBe(prefetchEnded.Task);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 2000 && !condition(); attempt++)
        {
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }

        condition().ShouldBeTrue();
    }

    private async Task<IngestOutcome> Ingest(params Data[] wallpapers)
    {
        using var client = new HttpClient();

        return await service.IngestPageAsync(wallpapers, IngestionRuns.Create(new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("some-directory", "famous-some-directory", ""), "resolved-category"), client, fileRepository, []), progress));
    }

    private static Data CreateWallpaper(string id, string path = "")
        => new(id, 0, 0, 0, "", path);

    private sealed class FakeNewWallpaperIngestor : INewWallpaperIngestor
    {
        private readonly Lock gate = new();
        private readonly List<string> fetched = [];
        private readonly List<(Data Wallpaper, string Extension, IngestionRun Run)> ingested = [];
        private readonly Dictionary<string, IReadOnlyList<Tag>> tagsIngested = [];
        private int concurrentFetches;
        private int concurrentIngests;

        public IReadOnlyList<string> Fetched
        {
            get
            {
                lock (gate) return [.. fetched];
            }
        }

        public IReadOnlyList<(Data Wallpaper, string Extension, IngestionRun Run)> Ingested
        {
            get
            {
                lock (gate) return [.. ingested];
            }
        }

        public IReadOnlyDictionary<string, IReadOnlyList<Tag>> TagsIngested
        {
            get
            {
                lock (gate) return new Dictionary<string, IReadOnlyList<Tag>>(tagsIngested);
            }
        }

        public HashSet<string> FailedFetches { get; } = [];

        public HashSet<string> FailedIngests { get; } = [];

        public int MaximumConcurrentFetches { get; private set; }

        public int MaximumConcurrentIngests { get; private set; }

        public Func<Data, CancellationToken, Task> OnFetch { get; set; } = (_, _) => Task.CompletedTask;

        public Func<Data, CancellationToken, Task> OnIngest { get; set; } = (_, _) => Task.CompletedTask;

        public static Tag TagFor(Data wallpaper) => new(1, wallpaper.Id, wallpaper.Id, 1, "Other Figures", "sfw");

        public async Task<Option<IReadOnlyList<Tag>>> FetchTagsAsync(Data wallpaper, IngestionRun run)
        {
            lock (gate)
            {
                fetched.Add(wallpaper.Id);
                MaximumConcurrentFetches = Math.Max(MaximumConcurrentFetches, ++concurrentFetches);
            }

            try
            {
                await OnFetch(wallpaper, run.CancellationToken);

                return FailedFetches.Contains(wallpaper.Id) ? Option.None<IReadOnlyList<Tag>>() : Option.Some<IReadOnlyList<Tag>>([TagFor(wallpaper)]);
            }
            finally
            {
                lock (gate) concurrentFetches--;
            }
        }

        public async Task<IngestOutcome> IngestAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, IngestionRun run)
        {
            lock (gate) MaximumConcurrentIngests = Math.Max(MaximumConcurrentIngests, ++concurrentIngests);

            try
            {
                await OnIngest(candidate.Wallpaper, run.CancellationToken);
                lock (gate)
                {
                    ingested.Add((candidate.Wallpaper, candidate.Extension, run));
                    tagsIngested[candidate.Wallpaper.Id] = tags;
                }

                return FailedIngests.Contains(candidate.Wallpaper.Id) ? IngestOutcome.Incomplete : IngestOutcome.Complete;
            }
            finally
            {
                lock (gate) concurrentIngests--;
            }
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
