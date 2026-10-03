using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Tags;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Loads the stored tags for the main window, reporting problems to the user instead of throwing, and creates the editor for them.</summary>
/// <param name="catalogue">The source of the tags and where edits are saved.</param>
/// <param name="status">Where problems are reported.</param>
public sealed class TagsBrowser(ITagCatalogue catalogue, StatusReporter status)
{
    /// <summary>Creates the tags editor window over every stored tag; none, with the reason reported, if they could not be listed or there are none.</summary>
    public async Task<Option<TagsEditorWindow>> CreateEditorAsync() =>
        (await catalogue.ListAsync()).Match(OpenOnTags, exception =>
        {
            status.Error("Unable to list tags.", exception);

            return Option.None<TagsEditorWindow>();
        });

    private Option<TagsEditorWindow> OpenOnTags(IReadOnlyList<TagSummary> tags)
    {
        if (tags.Count > 0) return Option.Some(new TagsEditorWindow([.. tags.Select(tag => new TagRow(tag))], catalogue));

        status.Append("There are no tags to edit.");

        return Option.None<TagsEditorWindow>();
    }
}
