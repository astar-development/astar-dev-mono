using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

/// <summary>
/// Drives a single account card in the left-hand account panel.
/// </summary>
public sealed partial class AccountCardViewModel : ObservableObject
{
    private readonly OneDriveAccount _model;
    private readonly ILocalizationService _localizationService;

    private static readonly string[] _palette =
    [
        "#185FA5",
        "#0F6E56",
        "#993C1D",
        "#534AB7",
        "#993556",
        "#854F0B"
    ];

    /// <summary>Raw string account ID — unwrapped at the display boundary.</summary>
    public string Id => _model.Id.Id;
    public string DisplayName => _model.Profile.DisplayName;
    public string Email => _model.Profile.Email;
    public Color AccentColor => Color.Parse(PaletteHex(_model.AccentIndex));

    /// <summary>
    /// Two-letter initials derived from DisplayName (e.g. "JS" for "Jason Smith").
    /// Falls back to the first character of the email address.
    /// </summary>
    public string Initials
    {
        get
        {
            string[] parts = _model.Profile.DisplayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : _model.Profile.DisplayName.Length > 0
                    ? _model.Profile.DisplayName[0].ToString().ToUpperInvariant()
                    : _model.Profile.Email.Length > 0
                        ? _model.Profile.Email[0].ToString().ToUpperInvariant()
                        : "?";
        }
    }

    /// <summary>
    /// Accent colour index (0–5) used to pick the avatar background colour.
    /// Resolves to one of the AccountAccent0–5 resources defined in Base.axaml.
    /// </summary>
    public int AccentIndex => _model.AccentIndex;

    /// <summary>Hex string for the accent colour, looked up from the fixed palette.</summary>
    public string AccentHex => _palette[_model.AccentIndex % _palette.Length];

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial SyncState SyncState { get; set; } = SyncState.Idle;

    [ObservableProperty]
    public partial int ConflictCount { get; set; }

    [ObservableProperty]
    public partial string LastSyncText { get; set; } = string.Empty;

    /// <summary>Raised when the user clicks the card — navigates to Files view.</summary>
    public event EventHandler<AccountCardViewModel>? Selected;

    /// <summary>Raised when the user requests account removal.</summary>
    public event EventHandler<AccountCardViewModel>? RemoveRequested;

    [RelayCommand]
    private void Select() => Selected?.Invoke(this, this);

    [RelayCommand]
    private void Remove() => RemoveRequested?.Invoke(this, this);

    public AccountCardViewModel(OneDriveAccount model, ILocalizationService localizationService)
    {
        _model = model;
        _localizationService = localizationService;
        IsActive = model.IsActive;
        UpdateLastSyncText();
    }

    /// <summary>Refreshes observable properties from the underlying model.</summary>
    public void RefreshFromModel()
    {
        IsActive = _model.IsActive;
        UpdateLastSyncText();
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(Initials));
    }

    private void UpdateLastSyncText()
    {
        if (_model.LastSyncedAt is not Option<DateTimeOffset>.Some lastSyncedAt)
        {
            LastSyncText = _localizationService.GetLocal("Common.NeverSynced");
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - lastSyncedAt.Value;
        LastSyncText = elapsed.TotalMinutes < 2 ? _localizationService.GetLocal("Common.JustNow")
                     : elapsed.TotalHours < 1 ? _localizationService.GetLocal("Common.MinutesAgo", (int)elapsed.TotalMinutes)
                     : elapsed.TotalDays < 1 ? _localizationService.GetLocal("Common.HoursAgo", (int)elapsed.TotalHours)
                     : elapsed.TotalDays < 2 ? _localizationService.GetLocal("Common.Yesterday")
                     : _localizationService.GetLocal("Common.DaysAgo", (int)elapsed.TotalDays);
    }

    /// <summary>Returns the palette colour for the given index.</summary>
    public static Color PaletteColor(int index) => Color.Parse(_palette[index % _palette.Length]);

    /// <summary>Returns the palette hex string for the given index.</summary>
    public static string PaletteHex(int index) => _palette[index % _palette.Length];
}
