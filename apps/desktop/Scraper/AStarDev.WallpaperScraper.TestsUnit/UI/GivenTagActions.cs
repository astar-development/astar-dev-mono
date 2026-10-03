using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Operations;
using AStarDev.WallpaperScraper.Tags;
using AStarDev.WallpaperScraper.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.WallpaperScraper.TestsUnit.UI;

public sealed class GivenTagActions : IDisposable
{
    private readonly OperationCoordinator coordinator = new();
    private readonly StatusReporter status = TestStatusReporter.Create();
    private readonly FakeDialogHost dialogs = new();
    private readonly FakeTagCatalogue tagCatalogue = new();
    private readonly TagActions actions;

    public GivenTagActions()
        => actions = new(new UserOperationRunner(coordinator, status), new TagsBrowser(tagCatalogue, status));

    public void Dispose() => coordinator.Dispose();

    [Fact]
    public async Task when_there_are_no_tags_to_edit_then_no_editor_is_shown_and_the_user_is_told()
    {
        await actions.EditTagsAsync(dialogs);

        (dialogs.ShownCount, status.Text).ShouldBe((0, $"{TestStatusReporter.Timestamp} There are no tags to edit."));
    }

    [Fact]
    public async Task when_the_tags_editor_cannot_be_prepared_then_the_failure_is_reported_and_not_thrown()
    {
        tagCatalogue.Failure = new InvalidOperationException("tags broke");

        await Should.NotThrowAsync(() => actions.EditTagsAsync(dialogs));

        (dialogs.ShownCount, status.Text).ShouldBe((0, $"{TestStatusReporter.Timestamp} Unable to edit tags. tags broke"));
    }

    private sealed class FakeTagCatalogue : ITagCatalogue
    {
        public Exception? Failure { get; set; }

        public Task<Exceptional<IReadOnlyList<TagSummary>>> ListAsync(CancellationToken cancellationToken = default)
            => Failure is null
                ? Task.FromResult(Exceptional.Success<IReadOnlyList<TagSummary>>([]))
                : Task.FromException<Exceptional<IReadOnlyList<TagSummary>>>(Failure);

        public Task<Exceptional<Unit>> SaveFlagsAsync(IReadOnlyDictionary<int, TagFlags> flagsByWallhavenId, CancellationToken cancellationToken = default)
            => Task.FromResult(Exceptional.Success(Unit.Instance));
    }
}
