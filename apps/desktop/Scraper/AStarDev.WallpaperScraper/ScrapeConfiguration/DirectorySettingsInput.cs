using System.IO.Abstractions;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>The unvalidated scrape directories as entered in the configuration editor.</summary>
/// <param name="RootDirectory">The entered root directory.</param>
/// <param name="RootDirectoryFamous">The entered famous root directory.</param>
/// <param name="SubDirectoryName">The entered subdirectory name.</param>
public sealed record DirectorySettingsInput(string RootDirectory, string RootDirectoryFamous, string SubDirectoryName)
{
    /// <summary>Creates the editor input from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static DirectorySettingsInput From(ScrapeConfigurationEntity entity)
    {
        var settings = DirectorySettings.From(entity);

        return new DirectorySettingsInput(settings.RootDirectory, settings.RootDirectoryFamous, settings.SubDirectoryName);
    }

    /// <summary>Validates the input: both root directories must be non-blank. Surrounding whitespace is trimmed.</summary>
    /// <returns>The validated <see cref="DirectorySettings"/>, or every validation error found.</returns>
    public Validation<DirectorySettings> Validate()
    {
        List<ValidationError> errors = [];
        RequireNotBlank(nameof(RootDirectory), RootDirectory, errors);
        RequireNotBlank(nameof(RootDirectoryFamous), RootDirectoryFamous, errors);

        return errors.Count > 0
            ? Validation.Invalid<DirectorySettings>(errors)
            : Validation.Valid(new DirectorySettings(RootDirectory.Trim(), RootDirectoryFamous.Trim(), SubDirectoryName.Trim()));
    }

    /// <summary>Describes each non-blank root directory that does not exist. This is advisory only and never blocks saving.</summary>
    /// <param name="fileSystem">The file system to check.</param>
    public IReadOnlyList<string> MissingDirectories(IFileSystem fileSystem)
    {
        List<string> missing = [];
        AddWhenMissing("Root directory", RootDirectory, fileSystem, missing);
        AddWhenMissing("Famous root directory", RootDirectoryFamous, fileSystem, missing);

        return missing;
    }

    private static void RequireNotBlank(string property, string value, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(ValidationErrorFactory.Create(property, "Must not be blank."));
    }

    private static void AddWhenMissing(string label, string value, IFileSystem fileSystem, List<string> missing)
    {
        var path = value.Trim();
        if (path.Length > 0 && !fileSystem.Directory.Exists(path)) missing.Add($"{label} does not exist: {path}");
    }
}
