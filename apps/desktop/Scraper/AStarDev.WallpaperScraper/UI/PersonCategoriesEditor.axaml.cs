using System.Collections.ObjectModel;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Lists, adds, renames and removes the person categories of a scrape configuration.</summary>
public sealed partial class PersonCategoriesEditor : UserControl
{
    private readonly ObservableCollection<PersonCategoryRow> rows = [];

    public PersonCategoriesEditor()
    {
        InitializeComponent();
        CategoriesList.ItemsSource = rows;
    }

    /// <summary>Fills the list from the specified categories.</summary>
    /// <param name="input">The categories to display.</param>
    public void Load(PersonCategoriesInput input)
    {
        rows.Clear();
        foreach (var category in input.Categories) rows.Add(new PersonCategoryRow(category));
    }

    /// <summary>Reads the categories currently entered in the list.</summary>
    public PersonCategoriesInput ReadInput() => new([.. rows.Select(row => row.ToInput())]);

    public void AddCategory(object? sender, RoutedEventArgs eventArgs) =>
        rows.Add(new PersonCategoryRow(new PersonCategoryInput(Option.None<Guid>(), string.Empty)));

    public void RemoveCategory(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Control { DataContext: PersonCategoryRow row }) _ = rows.Remove(row);
    }
}
