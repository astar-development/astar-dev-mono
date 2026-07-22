namespace AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;

/// <summary>Builds the editor view model for each table editable from the main window's Configuration menu.</summary>
public interface IEntityEditorFactory
{
    /// <summary>Creates the editor for the <c>ConnectionStrings</c> table.</summary>
    EntityEditorViewModelBase CreateConnectionStringsEditor();

    /// <summary>Creates the editor for the <c>FileClassificationCategories</c> table.</summary>
    EntityEditorViewModelBase CreateFileClassificationCategoriesEditor();

    /// <summary>Creates the editor for the <c>SearchConfiguration</c> table.</summary>
    EntityEditorViewModelBase CreateSearchConfigurationEditor();

    /// <summary>Creates the editor for the <c>ModelToIgnore</c> table.</summary>
    EntityEditorViewModelBase CreateModelToIgnoreEditor();

    /// <summary>Creates the editor for the <c>ScrapeDirectories</c> table.</summary>
    EntityEditorViewModelBase CreateScrapeDirectoriesEditor();

    /// <summary>
    ///     Creates the editor for the <c>SearchCategories</c> table. The app manages a single
    ///     <c>SearchConfiguration</c> row, so newly added rows are stamped with its <c>Id</c> on save rather than
    ///     exposing the foreign key as an editable column.
    /// </summary>
    EntityEditorViewModelBase CreateSearchCategoriesEditor();

    /// <summary>Creates the editor for the <c>TagToIgnore</c> table.</summary>
    EntityEditorViewModelBase CreateTagToIgnoreEditor();

    /// <summary>Creates the editor for the <c>UserConfiguration</c> table.</summary>
    EntityEditorViewModelBase CreateUserConfigurationEditor();
}
