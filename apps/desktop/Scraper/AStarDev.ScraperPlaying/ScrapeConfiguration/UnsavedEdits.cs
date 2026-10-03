using System.Diagnostics.CodeAnalysis;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Decides whether the configuration editor holds edits that have not been saved.</summary>
public static class UnsavedEdits
{
    /// <summary>Whether the editor's current contents differ from what was loaded.</summary>
    /// <param name="baseline">The inputs the editor was loaded with.</param>
    /// <param name="readCurrent">Reads the inputs currently entered in the editor, which can throw when a control holds a value that cannot be read.</param>
    /// <returns><see langword="true"/> when the contents differ or cannot be read; otherwise <see langword="false"/>.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Whatever stops the editor being read, the person has typed something that would be lost, so it counts as an unsaved edit.")]
    public static bool Exist(ConfigurationEditInputs baseline, Func<ConfigurationEditInputs> readCurrent)
    {
        try
        {
            return !IsSame(baseline, readCurrent());
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static bool IsSame(ConfigurationEditInputs baseline, ConfigurationEditInputs current)
        => baseline.Root == current.Root
            && baseline.User == current.User
            && baseline.Directories == current.Directories
            && baseline.Search == current.Search
            && baseline.SearchCategories.Categories.SequenceEqual(current.SearchCategories.Categories)
            && baseline.PersonCategories.Categories.SequenceEqual(current.PersonCategories.Categories);
}
