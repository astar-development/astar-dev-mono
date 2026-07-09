using System.Text.Json;

namespace AStar.Dev.Utilities;

/// <summary>
///     The <see cref="ObjectExtensions" /> class contains some useful methods to enable various tasks
///     to be performed in a more fluid, English sentence, style
/// </summary>
public static class ObjectExtensions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly JsonSerializerOptions OptionsWithoutNulls = new(JsonSerializerDefaults.Web) { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    /// <summary>
    ///     The ToJson method, as you might expect, converts the supplied object to its JSON equivalent (using the
    ///     JsonSerializerDefaults.Web defaults with WriteIndented set to true)
    /// </summary>
    /// <param name="obj">The object to convert to JSON</param>
    /// <returns>The JSON string of the object supplied</returns>
    public static string ToJson<T>(this T obj) =>
        JsonSerializer.Serialize(obj, Options);

    /// <summary>
    ///     The ToJsonWithoutNulls method converts the supplied object to its JSON equivalent, excluding null values (using the
    ///     JsonSerializerDefaults.Web defaults with WriteIndented set to true and ignore nulls when writing)
    /// </summary>
    /// <param name="obj">The object to convert to JSON</param>
    /// <returns>The JSON string of the object supplied</returns>
    public static string ToJsonWithoutNulls<T>(this T obj) =>
        JsonSerializer.Serialize(obj, OptionsWithoutNulls);
}
