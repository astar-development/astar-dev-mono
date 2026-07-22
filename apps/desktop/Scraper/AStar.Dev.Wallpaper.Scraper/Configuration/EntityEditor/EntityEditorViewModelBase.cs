using System.Collections;
using System.Windows.Input;
using AStar.Dev.Wallpaper.Scraper.ViewModels;
using ReactiveUI;

namespace AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;

/// <summary>
///     Non-generic base for <see cref="EntityEditorViewModel{TEntity}" />, allowing a single, non-generic
///     <c>EntityEditorWindow</c> to host an editor for any entity type.
/// </summary>
public abstract class EntityEditorViewModelBase : ViewModelBase
{
    private string statusMessage = string.Empty;

    /// <summary>The window title and menu text for this editor.</summary>
    public abstract string Title { get; }

    /// <summary>The rows currently displayed in the grid.</summary>
    public abstract IList Items { get; }

    /// <summary>
    ///     Whether this table allows an unlimited number of rows. When <c>false</c>, the table is a single-row
    ///     configuration table: <see cref="AddCommand" /> is only executable while <see cref="Items" /> is empty.
    /// </summary>
    public abstract bool CanAddRemove { get; }

    /// <summary>The row currently selected in the grid, if any.</summary>
    public abstract object? SelectedItem { get; set; }

    /// <summary>Adds a new, defaulted row.</summary>
    public abstract ICommand AddCommand { get; }

    /// <summary>Removes the currently selected row.</summary>
    public abstract ICommand DeleteCommand { get; }

    /// <summary>Persists all pending additions, edits and removals to the database.</summary>
    public abstract ICommand SaveCommand { get; }

    /// <summary>Replaces <see cref="Items" /> with the contents of the table's export JSON file.</summary>
    public abstract ICommand ImportCommand { get; }

    /// <summary>Writes the current <see cref="Items" /> to the table's export JSON file.</summary>
    public abstract ICommand ExportCommand { get; }

    /// <summary>Button text for the editor-specific action, if the editor provides one.</summary>
    public virtual string? CustomActionLabel => null;

    /// <summary>Runs the editor-specific action, if the editor provides one.</summary>
    public virtual ICommand? CustomActionCommand => null;

    /// <summary>Whether this editor provides an editor-specific action.</summary>
    public bool HasCustomAction => CustomActionCommand is not null;

    /// <summary>Feedback from the most recent Save, Import or Export command.</summary>
    public string StatusMessage
    {
        get => statusMessage;
        protected set => this.RaiseAndSetIfChanged(ref statusMessage, value);
    }

    /// <summary>Whether the named property should be shown as a grid column.</summary>
    /// <param name="propertyName">The CLR property name of the candidate column.</param>
    public abstract bool IsColumnVisible(string propertyName);

    /// <summary>Whether the named property's column should be non-editable.</summary>
    /// <param name="propertyName">The CLR property name of the candidate column.</param>
    public abstract bool IsColumnReadOnly(string propertyName);
}
