using System.Windows.Input;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>One choice in the display settings, e.g. a layout or a theme.</summary>
public sealed class DisplayOptionViewModel : LocalizedObservableObject
{
    private readonly string labelKey;
    private bool isSelected;

    /// <summary>Creates the option.</summary>
    /// <param name="labelKey">The localisation key of the option text.</param>
    /// <param name="select">Called when the user chooses this option.</param>
    /// <param name="localization">Supplies the text and raises changes when the culture changes.</param>
    public DisplayOptionViewModel(string labelKey, Action select, ILocalizationService localization) : base(localization)
    {
        this.labelKey = labelKey;
        SelectCommand = new RelayCommand(select);
    }

    /// <summary>The option text.</summary>
    public string Label => Localization.GetLocal(labelKey);

    /// <summary>Whether this option is the current choice.</summary>
    public bool IsSelected
    {
        get => isSelected;
        internal set => SetProperty(ref isSelected, value);
    }

    /// <summary>Makes this option the current choice.</summary>
    public ICommand SelectCommand { get; }
}
