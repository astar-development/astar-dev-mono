using System.Text.Json;

namespace AStar.Dev.Utilities;

/// <summary>
///     The <see cref="ObjectExtensions" /> class contains some useful methods to enable various tasks
///     to be performed in a more fluid, English sentence, style
/// </summary>
public static class ObjectExtensions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    ///     The ToJson method, as you might expect, converts the supplied object to its JSON equivalent (using the
    ///     JsonSerializerDefaults.Web defaults with WriteIndented set to true)
    /// </summary>
    /// <param name="object">The object to convert to JSON</param>
    /// <returns>The JSON string of the object supplied</returns>
    public static string ToJson<T>(this T @object) =>
        JsonSerializer.Serialize(@object, Options);
}