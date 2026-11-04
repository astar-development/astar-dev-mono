using Avalonia;
using Avalonia.Media;

namespace AStarDev.CloudSync;

/// <summary>
/// AppBuilder configuration shared by the CloudSync app and its rendering tests, so both draw text the same way.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// The font family every CloudSync control uses unless it sets its own.
    /// </summary>
    public const string DefaultFontFamilyName = "fonts:Inter#Inter";

    /// <summary>
    /// Registers the embedded Inter font collection and makes Inter the default font family.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static AppBuilder WithCloudSyncFonts(this AppBuilder builder) => builder.WithInterFont().With(new FontManagerOptions { DefaultFamilyName = DefaultFontFamilyName });
}
