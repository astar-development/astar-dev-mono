using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>One row of the term list.</summary>
public sealed class TermRowViewModel : LocalizedObservableObject
{
    private readonly ClassificationTerm term;
    private TermStatus status;
    private bool hasError;

    internal TermRowViewModel(ClassificationTerm term, string categoryName, int fileCount, ILocalizationService localization) : base(localization)
    {
        this.term = term;
        status = term.Status;
        CategoryName = categoryName;
        FileCount = fileCount;
    }

    /// <summary>The term id.</summary>
    public TermId Id => term.Id;

    /// <summary>The term text.</summary>
    public string Text => term.Text;

    /// <summary>The category name.</summary>
    public string CategoryName { get; }

    /// <summary>The localised source.</summary>
    public string SourceText => Localization.GetLocal(SourceKey());

    /// <summary>The number of files linked to the term.</summary>
    public int FileCount { get; }

    /// <summary>The file count formatted for the culture.</summary>
    public string FileCountText => FileCount.ToString("N0", Localization.CurrentCulture);

    /// <summary>Whether the term is active or ignored.</summary>
    public TermStatus Status => status;

    /// <summary>Whether the term is ignored.</summary>
    public bool IsIgnored => status is TermStatus.Ignored;

    /// <summary>The localised status.</summary>
    public string StatusText => Localization.GetLocal(IsIgnored ? "Classification.Terms.Status.Ignored" : "Classification.Terms.Status.Active");

    /// <summary>The label of the toggle button.</summary>
    public string ToggleText => Localization.GetLocal(IsIgnored ? "Classification.Terms.Unignore" : "Classification.Terms.Ignore");

    /// <summary>The localised error of the last toggle, or empty.</summary>
    public string ErrorText => hasError ? Localization.GetLocal("Classification.Terms.ToggleFailed") : string.Empty;

    /// <summary>Whether the last toggle failed.</summary>
    public bool HasError => hasError;

    internal Unit Apply(TermStatus newStatus)
    {
        status = newStatus;
        hasError = false;
        Changed();

        return Unit.Instance;
    }

    internal Unit Fail()
    {
        hasError = true;
        Changed();

        return Unit.Instance;
    }

    private string SourceKey() => term.Source switch
    {
        TermSource.Seed => "Classification.Terms.Source.Seed",
        TermSource.User => "Classification.Terms.Source.User",
        TermSource.Auto => "Classification.Terms.Source.Auto",
        _ => "Classification.Terms.Source.Suggested"
    };

    private void Changed()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsIgnored));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ToggleText));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorText));
    }
}
