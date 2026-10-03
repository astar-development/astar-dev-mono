using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Tags;
using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenATagsBrowser
{
    private readonly FakeTagCatalogue catalogue = new();
    private readonly StatusReporter status = TestStatusReporter.Create();
    private readonly TagsBrowser browser;

    public GivenATagsBrowser() => browser = new(catalogue, status);

    [Fact]
    public async Task when_there_are_no_tags_then_no_editor_is_created_and_the_user_is_told()
    {
        var editor = await browser.CreateEditorAsync();

        (editor is Option<TagsEditorWindow>.None, status.Text).ShouldBe((true, $"{TestStatusReporter.Timestamp} There are no tags to edit."));
    }

    [Fact]
    public async Task when_listing_the_tags_fails_then_no_editor_is_created_and_the_failure_is_reported()
    {
        catalogue.Tags = new InvalidOperationException("list failed");

        var editor = await browser.CreateEditorAsync();

        (editor is Option<TagsEditorWindow>.None, status.Text).ShouldBe((true, $"{TestStatusReporter.Timestamp} Unable to list tags. list failed"));
    }

    private sealed class FakeTagCatalogue : ITagCatalogue
    {
        public Exceptional<IReadOnlyList<TagSummary>> Tags { get; set; } = Exceptional.Success<IReadOnlyList<TagSummary>>([]);

        public Task<Exceptional<IReadOnlyList<TagSummary>>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(Tags);

        public Task<Exceptional<Unit>> SaveFlagsAsync(IReadOnlyDictionary<int, TagFlags> flagsByWallhavenId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Exceptional.Success(Unit.Instance));
    }
}
