using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Validates every section of an edited scrape configuration and, when all are valid, saves them together.</summary>
/// <param name="updater">The service that writes the validated sections.</param>
public sealed class ConfigurationEditSaver(IScrapeConfigurationUpdater updater)
{
    /// <summary>Validates <paramref name="inputs"/> and saves them to the configuration <paramref name="id"/>.</summary>
    /// <param name="id">The configuration being edited.</param>
    /// <param name="inputs">The unvalidated contents of every section.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the save.</param>
    /// <returns>Nothing when the edit was saved; otherwise the message to show the user: every validation error one per line, that the configuration no longer exists, or why saving failed.</returns>
    public async Task<Option<string>> SaveAsync(ScrapeConfigurationId id, ConfigurationEditInputs inputs, CancellationToken cancellationToken)
    {
        var root = inputs.Root.Validate();
        var user = inputs.User.Validate();
        var directories = inputs.Directories.Validate();
        var search = inputs.Search.Validate();
        var searchCategories = inputs.SearchCategories.Validate();
        var personCategories = inputs.PersonCategories.Validate();

        IReadOnlyList<ValidationError> errors = [.. ErrorsOf(root), .. ErrorsOf(user), .. ErrorsOf(directories), .. ErrorsOf(search), .. ErrorsOf(searchCategories), .. ErrorsOf(personCategories)];
        if (errors.Count > 0) return Option.Some(string.Join(Environment.NewLine, errors.Select(error => $"{error.Property}: {error.Message}")));

        IReadOnlyList<IScrapeConfigurationSectionEdit> edits = [.. ValueOf(root), .. ValueOf(user), .. ValueOf(directories), .. ValueOf(search), .. ValueOf(searchCategories), .. ValueOf(personCategories)];

        return (await updater.SaveAsync(id, edits, cancellationToken)).Match(
            saved => saved.Match(_ => Option.None<string>(), () => Option.Some("The scrape configuration no longer exists.")),
            exception => Option.Some($"Unable to save the scrape configuration. {exception.Message}"));
    }

    private static IReadOnlyList<ValidationError> ErrorsOf<T>(Validation<T> validation) =>
        validation is Invalid<T> invalid ? invalid.Errors : [];

    private static IEnumerable<IScrapeConfigurationSectionEdit> ValueOf<T>(Validation<T> validation) where T : IScrapeConfigurationSectionEdit =>
        validation is Valid<T> valid ? [valid.Value] : [];
}
