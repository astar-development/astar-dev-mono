using System.Diagnostics.CodeAnalysis;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Tags;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Lists the stored tags so the flags of each can be changed. Save writes the changed flags and closes with <c>true</c>, Cancel closes with <c>false</c>.</summary>
public sealed partial class TagsEditorWindow : Window
{
    private readonly IReadOnlyList<TagRow> rows = [];
    private readonly ITagCatalogue catalogue = null!;

    /// <summary>Initializes the window for the design-time previewer.</summary>
    public TagsEditorWindow() => InitializeComponent();

    /// <summary>Initializes a new instance of the <see cref="TagsEditorWindow"/> class over the specified tags.</summary>
    /// <param name="rows">The tags to edit.</param>
    /// <param name="catalogue">The service the changed flags are saved through.</param>
    public TagsEditorWindow(IReadOnlyList<TagRow> rows, ITagCatalogue catalogue)
    {
        InitializeComponent();
        this.rows = rows;
        this.catalogue = catalogue;
        TagsList.ItemsSource = rows;
    }

    public void FilterChanged(object? sender, TextChangedEventArgs eventArgs) =>
        TagsList.ItemsSource = rows.Where(row => row.Matches(FilterTextBox.Text ?? string.Empty)).ToList();

    public async void Save(object? sender, RoutedEventArgs eventArgs)
    {
        ErrorText.Text = string.Empty;
        SetSaving(true);
        try
        {
            var failure = await TrySaveAsync();
            if (failure is Option<string>.Some some) ErrorText.Text = some.Value;
            else Close(true);
        }
        finally
        {
            SetSaving(false);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Called from an async void handler: any exception escaping it would crash the application, so the failure is shown in the editor instead.")]
    private async Task<Option<string>> TrySaveAsync()
    {
        try
        {
            var changes = rows.Where(row => row.IsChanged).ToDictionary(row => row.WallhavenTagId, row => row.Flags);
            var result = await catalogue.SaveFlagsAsync(changes);

            return result.Match(_ => Option.None<string>(), exception => Option.Some($"Unable to save the tags. {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Option.Some($"Unable to save the tags. {exception.Message}");
        }
    }

    public void Cancel(object? sender, RoutedEventArgs eventArgs) => Close(false);

    private void SetSaving(bool isSaving)
    {
        SaveButton.IsEnabled = !isSaving;
        CancelButton.IsEnabled = !isSaving;
    }
}
