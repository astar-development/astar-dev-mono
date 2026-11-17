using System.Text;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Unicode helpers shared by the path, root-name and classification text rules.</summary>
public static class UnicodeText
{
    /// <summary>Composes the text to NFC. Text that cannot be composed (an unpaired surrogate) is returned unchanged, so it never throws.</summary>
    /// <param name="text">The text to compose.</param>
    /// <returns>The NFC text, or <paramref name="text"/> when composition is not possible.</returns>
    public static string Compose(string text) => Try.Run(() => text.Normalize(NormalizationForm.FormC)).Match(composed => composed, _ => text);
}
