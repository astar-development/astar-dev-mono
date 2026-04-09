using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;
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

    public string Id => _model.Id;
    public string DisplayName => _model.DisplayName;
    public string Email => _model.Email;
    public Color AccentColor => Color.Parse(PaletteHex(_model.AccentIndex));

    /// <summary>
    /// Two-letter initials derived from DisplayName (e.g. "JS" for "Jason Smith").
    /// Falls back to the first character of the email address.
    /// </summary>
    public string Initials
    {
        get
        {
            string[] parts = _model.DisplayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : _model.DisplayName.Length > 0
                    ? _model.DisplayName[0].ToString().ToUpperInvariant()
                    : _model.Email.Length > 0
                        ? _model.Email[0].ToString().ToUpperInvariant()
                        : "?";
        }
    }

    /// <summary>
    /// Accent colour index (0–5) used to pick the avatar background colour.
    /// Resolves to one of the AccountAccent0–5 resources defined in Base.axaml.
    /// </summary>
    public int AccentIndex => _model.AccentIndex;

    /// <summary>Hex string for the accent colour, looked up from the fixed palette.</summary>
    public string AccentHex => AccentPalette[_model.AccentIndex % AccentPalette.Length];

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

    public AccountCardViewModel(OneDriveAccount model)
    {
        _model = model;
        IsActive = model.IsActive;
        UpdateLastSyncText();
    }

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
        if(_model.LastSyncedAt is null)
        {
            LastSyncText = "Never synced";
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - _model.LastSyncedAt.Value;
        LastSyncText = elapsed.TotalMinutes < 2 ? "Just now"
                     : elapsed.TotalHours < 1 ? $"{(int)elapsed.TotalMinutes}m ago"
                     : elapsed.TotalDays < 1 ? $"{(int)elapsed.TotalHours}h ago"
                     : elapsed.TotalDays < 2 ? "Yesterday"
                     : $"{(int)elapsed.TotalDays}d ago";
    }

    private static readonly string[] AccentPalette =
    [
        "#185FA5",
        "#0F6E56",
        "#993C1D",
        "#534AB7",
        "#993556",
        "#854F0B"
    ];

    public static Color PaletteColor(int index) => Color.Parse(Palette[index % Palette.Length]);

    public static string PaletteHex(int index) => Palette[index % Palette.Length];

    private static readonly string[] Palette =
    [
        "#185FA5",
        "#0F6E56",
        "#993C1D",
        "#534AB7",
        "#993556",
        "#854F0B"
    ];
}
