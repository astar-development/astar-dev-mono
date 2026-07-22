using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

namespace AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;

/// <summary>
///     Hosts an <see cref="EntityEditorViewModelBase" />, rendering its <c>Items</c> as an auto-generated,
///     inline-editable grid.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class EntityEditorWindow : Window
{
    /// <summary>Initialises the window and wires up the grid's column generation.</summary>
    public EntityEditorWindow()
    {
        InitializeComponent();

        Grid.AutoGeneratingColumn += OnAutoGeneratingColumn;
    }

    private void OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (DataContext is not EntityEditorViewModelBase viewModel)
        {
            return;
        }

        if (!viewModel.IsColumnVisible(e.PropertyName))
        {
            e.Cancel = true;

            return;
        }

        if (viewModel.IsColumnReadOnly(e.PropertyName))
        {
            e.Column.IsReadOnly = true;
        }
    }
}
