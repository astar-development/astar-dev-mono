using AStarDev.CloudSync.ViewModels.Classification;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Classification;

/// <summary>The classification window's term list. Bind its DataContext to a <c>ClassificationTermListViewModel</c>.</summary>
public partial class ClassificationTermListContent : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public ClassificationTermListContent()
    {
        InitializeComponent();
        Header("TermHeader").Click += (_, _) => SortBy(TermSortColumnFactory.CreateTerm());
        Header("CategoryHeader").Click += (_, _) => SortBy(TermSortColumnFactory.CreateCategory());
        Header("SourceHeader").Click += (_, _) => SortBy(TermSortColumnFactory.CreateSource());
        Header("FileCountHeader").Click += (_, _) => SortBy(TermSortColumnFactory.CreateFileCount());
        Header("IgnoredHeader").Click += (_, _) => SortBy(TermSortColumnFactory.CreateIgnored());
    }

    private Button Header(string name) => this.FindControl<Button>(name)!;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void SortBy(TermSortColumn column) => (DataContext as ClassificationTermListViewModel)?.SortCommand.Execute(column);
}
