using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;
using AStarDev.WallpaperScraper.WallpaperIngestion;

namespace AStarDev.WallpaperScraper.TestsUnit.Scraping;

public sealed class GivenATagFetcherAndLinker
{
    private static readonly string[] PersonCategories = ["Celebrities"];

    private readonly FakeJsonResponseProcessor jsonResponseProcessor = new();
    private readonly FakeTagsQuery tagsQuery = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<TagEntity, TagId> tagRepository;
    private readonly FakeFileTagRepository fileTagRepository = new();
    private readonly CapturingProgress progress = new();
    private readonly TagFetcher fetcher;
    private readonly TagLinker linker;

    public GivenATagFetcherAndLinker()
    {
        tagRepository = unitOfWork.Register<TagEntity, TagId>();
        var flagStore = new TagFlagStore();
        fetcher = new(jsonResponseProcessor, tagsQuery, flagStore);
        linker = new(tagsQuery, unitOfWork, fileTagRepository, flagStore);
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_new_tag_then_it_is_created_and_linked()
    {
        var createdId = TagId.Create();
        tagRepository.Store = tag =>
        {
            tag.Id = createdId;

            return tag;
        };
        SetUpDetailResponse(CreateTag(wallhavenTagId: 1, name: "landscape"));

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        var created = tagRepository.Added.Single();
        (created.WallhavenTagId, created.Name).ShouldBe((1, "landscape"));
        fileTagRepository.Added.Single().TagId.ShouldBe(createdId);
        progress.Messages.ShouldContain("Fetching tags for wallpaper wallpaper-1.");
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_tag_that_already_exists_then_it_is_reused_and_linked()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 2, name: "space"));
        var existingTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 2, Name = "space" };
        tagsQuery.Existing[2] = existingTag;

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Added.ShouldBeEmpty();
        fileTagRepository.Added.Single().TagId.ShouldBe(existingTag.Id);
    }

    [Fact]
    public async Task when_the_same_tag_appears_twice_in_one_detail_response_then_it_is_linked_only_once()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 3, name: "duplicate"), CreateTag(wallhavenTagId: 3, name: "duplicate"));
        tagsQuery.Existing[3] = new TagEntity { Id = TagId.Create(), WallhavenTagId = 3, Name = "duplicate" };

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        fileTagRepository.Added.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_two_different_wallpapers_introduce_the_same_new_tag_then_it_is_created_once_and_cached_across_calls()
    {
        var tag = CreateTag(wallhavenTagId: 4, name: "shared");
        jsonResponseProcessor.Responses["wallpaper-a"] = CreateDetailResponse(tag);
        jsonResponseProcessor.Responses["wallpaper-b"] = CreateDetailResponse(tag);
        // The tags query never sees the other call's not-yet-saved insert, simulating the production race: both lookups return "not found".
        var createdId = TagId.Create();
        tagRepository.Store = created =>
        {
            created.Id = createdId;

            return created;
        };
        var firstFileId = FileId.Create();
        var secondFileId = FileId.Create();

        var firstResult = await FetchAndLink("wallpaper-a", firstFileId);
        var secondResult = await FetchAndLink("wallpaper-b", secondFileId);

        firstResult.Match(_ => true, ex => throw ex).ShouldBeTrue();
        secondResult.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Added.Single().WallhavenTagId.ShouldBe(4);
        fileTagRepository.Added.Select(fileTag => (fileTag.FileId, fileTag.TagId)).ShouldBe([(firstFileId, createdId), (secondFileId, createdId)]);
    }

    [Fact]
    public async Task when_a_wallpaper_has_several_tags_then_only_the_tags_stored_at_the_start_of_the_run_are_looked_up_with_a_single_query()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 5, name: "one"), CreateTag(wallhavenTagId: 6, name: "two"), CreateTag(wallhavenTagId: 5, name: "one"), CreateTag(wallhavenTagId: 7, name: "three"));
        tagsQuery.Existing[6] = new TagEntity { Id = TagId.Create(), WallhavenTagId = 6, Name = "two" };

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        (tagsQuery.Queries.Count, string.Join(",", tagsQuery.Queries.Single().Order()), string.Join(",", tagRepository.Added.Select(tag => tag.WallhavenTagId).Order())).ShouldBe((1, "6", "5,7"));
    }

    [Fact]
    public async Task when_no_tag_of_a_wallpaper_is_stored_then_no_lookup_query_is_made()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 12, name: "brand new"));

        _ = await Run();

        tagsQuery.Queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_every_tag_of_a_later_wallpaper_is_already_cached_then_no_query_is_made()
    {
        var tag = CreateTag(wallhavenTagId: 8, name: "cached");
        tagsQuery.Existing[8] = new TagEntity { Id = TagId.Create(), WallhavenTagId = 8, Name = "cached" };
        jsonResponseProcessor.Responses["wallpaper-a"] = CreateDetailResponse(tag);
        jsonResponseProcessor.Responses["wallpaper-b"] = CreateDetailResponse(tag);

        _ = await FetchAndLink("wallpaper-a", FileId.Create());
        _ = await FetchAndLink("wallpaper-b", FileId.Create());

        tagsQuery.Queries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_tags_are_fetched_then_they_are_returned_with_their_categories()
    {
        SetUpDetailResponse(new Tag(9, "Max Verstappen", "max-verstappen", 51, "Other Figures", "sfw"));

        using var client = new HttpClient();

        var result = await fetcher.FetchTagsAsync("wallpaper-1", CreateRun(client));

        result.Match(tags => tags, ex => throw ex).ShouldBe([new Tag(9, "Max Verstappen", "max-verstappen", 51, "Other Figures", "sfw")]);
        tagRepository.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_fetched_tag_is_flagged_to_ignore_images_then_it_is_returned_flagged()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 20, name: "unwanted"), CreateTag(wallhavenTagId: 21, name: "wanted"));
        tagsQuery.Ignored.Add(20);

        using var client = new HttpClient();
        var result = await fetcher.FetchTagsAsync("wallpaper-1", CreateRun(client));

        result.Match(tags => tags.Select(tag => (tag.Id, tag.IgnoreImage)).ToList(), ex => throw ex).ShouldBe([(20, true), (21, false)]);
    }

    [Fact]
    public async Task when_a_fetched_tag_is_flagged_as_a_name_then_it_is_returned_flagged()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 24, name: "some name"), CreateTag(wallhavenTagId: 25, name: "outside"));
        tagsQuery.Names.Add(24);

        using var client = new HttpClient();
        var result = await fetcher.FetchTagsAsync("wallpaper-1", CreateRun(client));

        result.Match(tags => tags.Select(tag => (tag.Id, tag.IsName)).ToList(), ex => throw ex).ShouldBe([(24, true), (25, false)]);
    }

    [Fact]
    public async Task when_a_stored_tag_is_flagged_famous_then_it_is_returned_famous_whatever_its_category()
    {
        tagsQuery.Existing[30] = new TagEntity { WallhavenTagId = 30, Name = "plain", Category = "Nature", IsFamous = true };
        SetUpDetailResponse(new Tag(30, "plain", "plain", 1, "Nature", "sfw"));

        using var client = new HttpClient();
        var result = await fetcher.FetchTagsAsync("wallpaper-1", CreateRun(client));

        result.Match(tags => tags.Single().IsFamous, ex => throw ex).ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_stored_tag_is_not_flagged_famous_then_it_stays_not_famous_even_if_it_looks_like_a_person()
    {
        tagsQuery.Existing[31] = new TagEntity { WallhavenTagId = 31, Name = "Emma Watson", Category = "Celebrities", IsFamous = false };
        SetUpDetailResponse(new Tag(31, "Emma Watson", "emma-watson", 1, "Celebrities", "sfw"));

        using var client = new HttpClient();
        var result = await fetcher.FetchTagsAsync("wallpaper-1", CreateRun(client));

        result.Match(tags => tags.Single().IsFamous, ex => throw ex).ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_tag_is_not_stored_yet_then_it_is_seeded_famous_from_its_person_category()
    {
        SetUpDetailResponse(new Tag(32, "Emma Watson", "emma-watson", 1, "Celebrities", "sfw"), new Tag(33, "outside", "outside", 1, "Nature", "sfw"));

        using var client = new HttpClient();
        var result = await fetcher.FetchTagsAsync("wallpaper-1", CreateRun(client));

        result.Match(tags => tags.Select(tag => (tag.Id, tag.IsFamous)).ToList(), ex => throw ex).ShouldBe([(32, true), (33, false)]);
    }

    [Fact]
    public async Task when_a_new_famous_tag_is_linked_then_it_is_stored_famous()
    {
        SetUpDetailResponse(new Tag(34, "Emma Watson", "emma-watson", 1, "Celebrities", "sfw"));

        _ = await Run();

        tagRepository.Added.Single().IsFamous.ShouldBeTrue();
    }

    [Fact]
    public async Task when_tags_are_fetched_for_several_wallpapers_then_the_flags_are_loaded_with_a_single_query()
    {
        jsonResponseProcessor.Responses["wallpaper-a"] = CreateDetailResponse(CreateTag(wallhavenTagId: 22, name: "one"));
        jsonResponseProcessor.Responses["wallpaper-b"] = CreateDetailResponse(CreateTag(wallhavenTagId: 23, name: "two"));

        using var client = new HttpClient();
        _ = await fetcher.FetchTagsAsync("wallpaper-a", CreateRun(client));
        _ = await fetcher.FetchTagsAsync("wallpaper-b", CreateRun(client));

        tagsQuery.FlagsQueryCount.ShouldBe(1);
    }
    [Fact]
    public async Task when_adding_a_link_fails_part_way_then_everything_added_for_the_wallpaper_is_removed_again()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 1, name: "one"), CreateTag(wallhavenTagId: 2, name: "two"));
        fileTagRepository.FailingAdd = 2;

        var result = await Run();

        result.Match(_ => false, _ => true).ShouldBeTrue();
        (tagRepository.Added.Except(tagRepository.Deleted).Count(), fileTagRepository.Added.Except(fileTagRepository.Deleted).Count()).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_linking_failed_part_way_then_a_later_wallpaper_adds_the_removed_tag_again()
    {
        jsonResponseProcessor.Responses["wallpaper-a"] = CreateDetailResponse(CreateTag(wallhavenTagId: 9, name: "retried"));
        jsonResponseProcessor.Responses["wallpaper-b"] = CreateDetailResponse(CreateTag(wallhavenTagId: 9, name: "retried"));
        fileTagRepository.FailingAdd = 1;

        _ = await FetchAndLink("wallpaper-a", FileId.Create());
        _ = await FetchAndLink("wallpaper-b", FileId.Create());

        (tagRepository.Added.Except(tagRepository.Deleted).Count(), fileTagRepository.Added.Except(fileTagRepository.Deleted).Count()).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_the_detail_fetch_fails_then_the_failure_is_returned_not_thrown()
    {
        var exception = new HttpRequestException("boom");
        jsonResponseProcessor.Failure = Option.Some<Exception>(exception);

        var result = await Run();

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
        tagRepository.Added.ShouldBeEmpty();
        fileTagRepository.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_detail_fetch_fails_then_the_original_throw_site_is_kept_in_the_stack_trace()
    {
        jsonResponseProcessor.Failure = Option.Some<Exception>(ThrownFailure.Create("boom"));

        var result = await Run();

        result.Match(_ => string.Empty, ThrownFailure.TraceOf).ShouldContain(ThrownFailure.ThrowSite);
    }

    [Fact]
    public async Task when_a_wallpaper_has_no_tags_then_it_is_a_no_op_success()
    {
        SetUpDetailResponse();

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Added.ShouldBeEmpty();
        fileTagRepository.Added.ShouldBeEmpty();
    }

    private Task<Exceptional<Unit>> Run()
        => FetchAndLink("wallpaper-1", FileId.Create());

    private async Task<Exceptional<Unit>> FetchAndLink(string wallpaperId, FileId fileId)
    {
        using var client = new HttpClient();

        return await (await fetcher.FetchTagsAsync(wallpaperId, CreateRun(client)))
            .Match(tags => linker.LinkTagsAsync(fileId, tags, CancellationToken.None), exception => Task.FromResult((Exceptional<Unit>)exception));
    }

    private IngestionRun CreateRun(HttpClient client)
        => IngestionRuns.Create(new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("root", "famous", "segment"), "category"), client, new FakeRepository<FileEntity, FileId>(), PersonCategories), progress);

    private void SetUpDetailResponse(params Tag[] tags)
        => jsonResponseProcessor.Responses["wallpaper-1"] = CreateDetailResponse(tags);

    private static DetailResponse CreateDetailResponse(params Tag[] tags)
        => new(new Data(tags));

    private static Tag CreateTag(int wallhavenTagId, string name)
        => new(wallhavenTagId, name, name, 1, "Nature", "sfw");

    private sealed class FakeJsonResponseProcessor : IJsonResponseProcessor
    {
        public Dictionary<string, DetailResponse> Responses { get; } = [];

        public Option<Exception> Failure { get; set; } = Option.None<Exception>();

        public Task<Exceptional<Option<T>>> GetFromJsonAsync<T>(Uri url, HttpClient client, CancellationToken cancellationToken)
        {
            if (Failure is Option<Exception>.Some failure) return Task.FromResult<Exceptional<Option<T>>>(failure.Value);

            var wallpaperId = url.OriginalString[ApplicationConstants.WallhavenDetailPathTemplate.Length..];
            Option<T> response = Responses.TryGetValue(wallpaperId, out var detail) ? (Option<T>)(T)(object)detail : Option<T>.None.Instance;

            return Task.FromResult<Exceptional<Option<T>>>(response);
        }
    }

    private sealed class FakeTagsQuery : ITagsQuery
    {
        public Dictionary<int, TagEntity> Existing { get; } = [];

        public List<IReadOnlyCollection<int>> Queries { get; } = [];

        public HashSet<int> Ignored { get; } = [];

        public HashSet<int> Names { get; } = [];

        public int FlagsQueryCount { get; private set; }

        public Task<Exceptional<IReadOnlyList<TagFlagProjection>>> GetFlagsAsync(CancellationToken cancellationToken = default)
        {
            FlagsQueryCount++;
            IReadOnlyList<TagFlagProjection> flags = [.. Existing.Values.Select(tag => new TagFlagProjection(tag.WallhavenTagId, Ignored.Contains(tag.WallhavenTagId), Names.Contains(tag.WallhavenTagId), tag.IsFamous))
                .Concat(Ignored.Where(id => !Existing.ContainsKey(id)).Select(id => new TagFlagProjection(id, true, Names.Contains(id), false)))
                .Concat(Names.Where(id => !Existing.ContainsKey(id) && !Ignored.Contains(id)).Select(id => new TagFlagProjection(id, false, true, false)))];

            return Task.FromResult<Exceptional<IReadOnlyList<TagFlagProjection>>>(Result(flags));
        }

        public Task<Exceptional<IReadOnlyList<TagEntity>>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Exceptional<IReadOnlyList<TagEntity>>>(Existing.Values.ToList());

        private static Exceptional<T> Result<T>(T value) => value;

        public Task<Exceptional<IReadOnlyList<TagEntity>>> FindByWallhavenIdsAsync(IReadOnlyCollection<int> wallhavenTagIds, CancellationToken cancellationToken = default)
        {
            Queries.Add(wallhavenTagIds);

            return Task.FromResult<Exceptional<IReadOnlyList<TagEntity>>>(wallhavenTagIds.Where(Existing.ContainsKey).Select(id => Existing[id]).ToList());
        }
    }

    private sealed class FakeFileTagRepository : IFileTagRepository
    {
        private int addCount;

        public List<FileTagEntity> Added { get; } = [];

        public List<FileTagEntity> Deleted { get; } = [];

        /// <summary>The 1-based call to <see cref="Add"/> that fails, or 0 for none.</summary>
        public int FailingAdd { get; set; }

        public Exceptional<FileTagEntity> Add(FileTagEntity fileTag)
        {
            addCount++;
            if (addCount == FailingAdd) return new InvalidOperationException("add failed");

            Added.Add(fileTag);

            return fileTag;
        }

        public Exceptional<Unit> Delete(FileTagEntity fileTag)
        {
            Deleted.Add(fileTag);

            return Unit.Instance;
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
