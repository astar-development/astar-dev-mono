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
    private readonly OneDriveAccount model;
    private readonly ILocalizationService localizationService;

    private static readonly string[] palette =
    [
        "#185FA5",
        "#0F6E56",
        "#993C1D",
        "#534AB7",
        "#993556",
        "#854F0B"
    ];

    /// <summary>Raw string account ID — unwrapped at the display boundary.</summary>
    public string Id => model.Id.Id;
    public string DisplayName => model.Profile.DisplayName;
    public string Email => model.Profile.Email;
    public Color AccentColor => Color.Parse(PaletteHex(model.AccentIndex));

    /// <summary>
    /// Two-letter initials derived from DisplayName (e.g. "JS" for "Jason Smith").
    /// Falls back to the first character of the email address.
    /// </summary>
    public string Initials
    {
        get
        {
            string[] parts = model.Profile.DisplayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : model.Profile.DisplayName.Length > 0
                    ? model.Profile.DisplayName[0].ToString().ToUpperInvariant()
                    : model.Profile.Email.Length > 0
                        ? model.Profile.Email[0].ToString().ToUpperInvariant()
                        : "?";
        }
    }

    /// <summary>
    /// Accent colour index (0–5) used to pick the avatar background colour.
    /// Resolves to one of the AccountAccent0–5 resources defined in Base.axaml.
    /// </summary>
    public int AccentIndex => model.AccentIndex;

    /// <summary>Hex string for the accent colour, looked up from the fixed palette.</summary>
    public string AccentHex => palette[model.AccentIndex % palette.Length];

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReAuthRequired))]
    public partial SyncState SyncState { get; set; } = SyncState.Idle;

    public bool IsReAuthRequired => SyncState == SyncState.ReAuthRequired;

    [ObservableProperty]
    public partial int ConflictCount { get; set; }

    [ObservableProperty]
    public partial string LastSyncText { get; set; } = string.Empty;

    /// <summary>Raised when the user clicks the card — navigates to Files view.</summary>
    public event EventHandler<AccountCardViewModel>? Selected;

    /// <summary>Raised when the user requests account removal.</summary>
    public event EventHandler<AccountCardViewModel>? RemoveRequested;

    /// <summary>Raised when the user requests re-authentication after a token failure.</summary>
    public event EventHandler<AccountCardViewModel>? ReAuthenticateRequested;

    [RelayCommand]
    private void Select() => Selected?.Invoke(this, this);

    [RelayCommand]
    private void Remove() => RemoveRequested?.Invoke(this, this);

    [RelayCommand]
    private void ReAuthenticate() => ReAuthenticateRequested?.Invoke(this, this);

    public AccountCardViewModel(OneDriveAccount model, ILocalizationService localizationService)
    {
        this.model = model;
        this.localizationService = localizationService;
        IsActive = model.IsActive;
        UpdateLastSyncText();
    }

    /// <summary>Refreshes observable properties from the underlying model.</summary>
    public void RefreshFromModel()
    {
        IsActive = model.IsActive;
        UpdateLastSyncText();
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(Initials));
    }

    private void UpdateLastSyncText()
    {
        if (model.LastSyncedAt is not Option<DateTimeOffset>.Some lastSyncedAt)
        {
            LastSyncText = localizationService.GetLocal("Common.NeverSynced");
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - lastSyncedAt.Value;
        LastSyncText = elapsed.TotalMinutes < 2 ? localizationService.GetLocal("Common.JustNow")
                     : elapsed.TotalHours < 1 ? localizationService.GetLocal("Common.MinutesAgo", (int)elapsed.TotalMinutes)
                     : elapsed.TotalDays < 1 ? localizationService.GetLocal("Common.HoursAgo", (int)elapsed.TotalHours)
                     : elapsed.TotalDays < 2 ? localizationService.GetLocal("Common.Yesterday")
                     : localizationService.GetLocal("Common.DaysAgo", (int)elapsed.TotalDays);
    }

    /// <summary>Returns the palette colour for the given index.</summary>
    public static Color PaletteColor(int index) => Color.Parse(palette[index % palette.Length]);

    /// <summary>Returns the palette hex string for the given index.</summary>
    public static string PaletteHex(int index) => palette[index % palette.Length];
}
