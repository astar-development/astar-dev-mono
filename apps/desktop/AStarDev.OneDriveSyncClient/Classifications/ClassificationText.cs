using System.Globalization;
using AStarDev.OneDriveSyncClient.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>Localised display text for the file classification views and dialogs, refreshed when the culture changes.</summary>
public sealed class ClassificationText : ObservableObject
{
    private static readonly string[] allTextProperties =
    [
        nameof(TitleText),
        nameof(DescriptionText),
        nameof(FilterPlaceholderText),
        nameof(ExportText),
        nameof(ImportText),
        nameof(EmptyText),
        nameof(FamousText),
        nameof(InternetText),
        nameof(EditText),
        nameof(DeleteText),
        nameof(NewSubCategoryPlaceholderText),
        nameof(AddSubCategoryText),
        nameof(AddCategoryText),
        nameof(CategoryNamePlaceholderText),
        nameof(AddText),
        nameof(ExportDialogTitleText),
        nameof(ImportDialogTitleText),
        nameof(ImportConfirmMessageText),
        nameof(EditDialogTitleText),
        nameof(EditDialogNameText),
        nameof(IncludeInSearchText),
        nameof(ParentText),
        nameof(CancelText),
        nameof(SaveText)
    ];

    private readonly ILocalizationService loc;

    internal ClassificationText(ILocalizationService loc)
    {
        this.loc = loc;
        loc.CultureChanged += OnCultureChanged;
    }

    /// <summary>Localised page title.</summary>
    public string TitleText => loc.GetLocal("Classifications.Title");

    /// <summary>Localised page description.</summary>
    public string DescriptionText => loc.GetLocal("Classifications.Description");

    /// <summary>Localised filter box placeholder.</summary>
    public string FilterPlaceholderText => loc.GetLocal("Classifications.FilterPlaceholder");

    /// <summary>Localised export button label.</summary>
    public string ExportText => loc.GetLocal("Classifications.Export");

    /// <summary>Localised import button label.</summary>
    public string ImportText => loc.GetLocal("Classifications.Import");

    /// <summary>Localised empty-state message.</summary>
    public string EmptyText => loc.GetLocal("Classifications.Empty");

    /// <summary>Localised famous flag label.</summary>
    public string FamousText => loc.GetLocal("Classifications.Famous");

    /// <summary>Localised internet flag label.</summary>
    public string InternetText => loc.GetLocal("Classifications.Internet");

    /// <summary>Localised edit button label.</summary>
    public string EditText => loc.GetLocal("Classifications.Edit");

    /// <summary>Localised delete button label.</summary>
    public string DeleteText => loc.GetLocal("Classifications.Delete");

    /// <summary>Localised new sub-category box placeholder.</summary>
    public string NewSubCategoryPlaceholderText => loc.GetLocal("Classifications.NewSubCategoryPlaceholder");

    /// <summary>Localised add sub-category button label.</summary>
    public string AddSubCategoryText => loc.GetLocal("Classifications.AddSubCategory");

    /// <summary>Localised add category heading.</summary>
    public string AddCategoryText => loc.GetLocal("Classifications.AddCategory");

    /// <summary>Localised category name box placeholder.</summary>
    public string CategoryNamePlaceholderText => loc.GetLocal("Classifications.CategoryNamePlaceholder");

    /// <summary>Localised add button label.</summary>
    public string AddText => loc.GetLocal("Classifications.Add");

    /// <summary>Localised export file dialog title.</summary>
    public string ExportDialogTitleText => loc.GetLocal("Classifications.ExportDialogTitle");

    /// <summary>Localised import file dialog title.</summary>
    public string ImportDialogTitleText => loc.GetLocal("Classifications.ImportDialogTitle");

    /// <summary>Localised import confirmation message.</summary>
    public string ImportConfirmMessageText => loc.GetLocal("Classifications.ImportConfirmMessage");

    /// <summary>Localised edit dialog title.</summary>
    public string EditDialogTitleText => loc.GetLocal("Classifications.EditDialog.Title");

    /// <summary>Localised edit dialog name label.</summary>
    public string EditDialogNameText => loc.GetLocal("Classifications.EditDialog.Name");

    /// <summary>Localised include in search label.</summary>
    public string IncludeInSearchText => loc.GetLocal("Classifications.EditDialog.IncludeInSearch");

    /// <summary>Localised parent label.</summary>
    public string ParentText => loc.GetLocal("Classifications.EditDialog.Parent");

    /// <summary>Localised cancel button label.</summary>
    public string CancelText => loc.GetLocal("Common.Cancel");

    /// <summary>Localised save button label.</summary>
    public string SaveText => loc.GetLocal("Common.Save");

    private void OnCultureChanged(object? sender, CultureInfo culture)
    {
        foreach (string propertyName in allTextProperties)
            OnPropertyChanged(propertyName);
    }
}
