using Avalonia;
using Avalonia.Controls;

namespace AStarDev.OneDriveSyncClient.Classifications;

public partial class CategoryEditDialog : Window
{
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly StyledProperty<ClassificationText?> TextProperty = AvaloniaProperty.Register<CategoryEditDialog, ClassificationText?>(nameof(Text));

    public CategoryEditDialog() => InitializeComponent();

    /// <summary>Localised display text for the dialog.</summary>
    public ClassificationText? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
