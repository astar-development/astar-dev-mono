using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Hosts the section tabs used to edit a single root scrape configuration. Save writes every section and closes with <c>true</c>, Cancel closes with <c>false</c>.</summary>
public sealed partial class ConfigurationEditorWindow : Window
{
    private readonly ConfigurationEditorSession session = null!;
    private readonly ConfigurationEditSaver saver = null!;
    private readonly IFileSystem fileSystem = null!;
    private ScrapeConfigurationId configurationId;
    private ConfigurationEditInputs loadedInputs = null!;

    /// <summary>Initializes the window for the design-time previewer.</summary>
    public ConfigurationEditorWindow() => InitializeComponent();

    /// <summary>Initializes a new instance of the <see cref="ConfigurationEditorWindow"/> class for the specified session.</summary>
    /// <param name="session">The configuration to edit first and how to switch to the others.</param>
    /// <param name="saver">The service that validates and saves the edits.</param>
    /// <param name="fileSystem">The file system used to check that the scrape directories exist.</param>
    public ConfigurationEditorWindow(ConfigurationEditorSession session, ConfigurationEditSaver saver, IFileSystem fileSystem)
    {
        InitializeComponent();
        this.session = session;
        this.saver = saver;
        this.fileSystem = fileSystem;
        Load(session.Initial);
        ConfigurationPicker.ItemsSource = session.Summaries;
        ConfigurationPicker.SelectedItem = session.Summaries.FirstOrDefault(summary => summary.Id == session.Initial.Id);
    }

    public async void ConfigurationSelected(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (ConfigurationPicker.SelectedItem is not ScrapeConfigurationSummary summary || summary.Id == configurationId) return;

        ShowError(string.Empty);
        var switched = await ConfigurationSwitch.ResolveAsync(summary, CanDiscardEditsAsync, session);
        if (switched.Loaded is Option<LoadedConfiguration>.Some loaded)
        {
            Load(loaded.Value);
        }
        else
        {
            ConfigurationPicker.SelectedItem = session.Summaries.FirstOrDefault(current => current.Id == configurationId);
            ShowError(switched.Error.Match(message => message, () => string.Empty));
        }
    }

    public async void Save(object? sender, RoutedEventArgs eventArgs)
    {
        ShowError(string.Empty);
        SetSaving(true);
        try
        {
            var failure = await TrySaveAsync();
            if (failure is Option<string>.Some some) ShowError(some.Value);
            else Close(true);
        }
        finally
        {
            SetSaving(false);
        }
    }

    public void Cancel(object? sender, RoutedEventArgs eventArgs) => Close(false);

    private Task<bool> CanDiscardEditsAsync() => UnsavedEdits.Exist(loadedInputs, ReadInputs)
        ? new ConfirmationWindow("Unsaved changes", "Switching configuration discards the edits made to this one.", "Discard").ShowDialog<bool>(this)
        : Task.FromResult(true);

    private void Load(LoadedConfiguration configuration)
    {
        configurationId = configuration.Id;
        loadedInputs = configuration.Inputs;
        RootSettingsTabContent.Load(configuration.Inputs.Root);
        UserTabContent.Load(configuration.Inputs.User);
        SearchTabContent.Load(configuration.Inputs.Search);
        SearchCategoriesTabContent.Load(configuration.Inputs.SearchCategories);
        PersonCategoriesTabContent.Load(configuration.Inputs.PersonCategories);
        DirectoriesTabContent.Load(configuration.Inputs.Directories, fileSystem);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Called from an async void handler: any exception escaping it would crash the application, so the failure is shown in the editor instead.")]
    private async Task<Option<string>> TrySaveAsync()
    {
        try
        {
            return await saver.SaveAsync(configurationId, ReadInputs(), CancellationToken.None);
        }
        catch (Exception exception)
        {
            return Option.Some($"Unable to save the scrape configuration. {exception.Message}");
        }
    }

    private ConfigurationEditInputs ReadInputs() => new(
        RootSettingsTabContent.ReadInput(),
        UserTabContent.ReadInput(),
        DirectoriesTabContent.ReadInput(),
        SearchTabContent.ReadInput(),
        SearchCategoriesTabContent.ReadInput(),
        PersonCategoriesTabContent.ReadInput());

    private void SetSaving(bool isSaving)
    {
        SaveButton.IsEnabled = !isSaving;
        CancelButton.IsEnabled = !isSaving;
    }

    private void ShowError(string message) => ErrorText.Text = message;
}
