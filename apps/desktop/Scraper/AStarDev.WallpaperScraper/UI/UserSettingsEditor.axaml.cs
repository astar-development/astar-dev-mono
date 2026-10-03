using AStarDev.WallpaperScraper.ScrapeConfiguration;
using Avalonia.Controls;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Edits the user settings of a scrape configuration.</summary>
public sealed partial class UserSettingsEditor : UserControl
{
    public UserSettingsEditor() => InitializeComponent();

    /// <summary>Fills the controls from the specified settings.</summary>
    /// <param name="input">The settings to display.</param>
    public void Load(UserSettingsInput input)
    {
        EmailAddressBox.Text = input.EmailAddress;
        UsernameBox.Text = input.Username;
        ApiKeyBox.Text = input.ApiKey;
    }

    /// <summary>Reads the settings currently entered in the controls.</summary>
    public UserSettingsInput ReadInput() => new(
        EmailAddressBox.Text ?? string.Empty,
        UsernameBox.Text ?? string.Empty,
        ApiKeyBox.Text ?? string.Empty);
}
