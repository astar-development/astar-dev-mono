using System.Collections.ObjectModel;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Lists, adds, edits and removes the search categories of a scrape configuration.</summary>
public sealed partial class SearchCategoriesEditor : UserControl
{
    private readonly ObservableCollection<SearchCategoryRow> rows = [];

    public SearchCategoriesEditor()
    {
        InitializeComponent();
        CategoriesList.ItemsSource = rows;
    }

    /// <summary>Fills the list from the specified categories.</summary>
    /// <param name="input">The categories to display.</param>
    public void Load(SearchCategoriesInput input)
    {
        rows.Clear();
        foreach (var category in input.Categories) rows.Add(new SearchCategoryRow(category));
    }

    /// <summary>Reads the categories currently entered in the list.</summary>
    public SearchCategoriesInput ReadInput() => new([.. rows.Select(row => row.ToInput())]);

    public void AddCategory(object? sender, RoutedEventArgs eventArgs) =>
        rows.Add(new SearchCategoryRow(new SearchCategoryInput(string.Empty, string.Empty, true, false, false, Option.None<SearchCategoryProgress>())));

    public void RemoveCategory(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Control { DataContext: SearchCategoryRow row }) _ = rows.Remove(row);
    }
}
