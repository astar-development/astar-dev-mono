using System.Text.Json;

namespace AStarDev.Utilities;

/// <summary>The <see cref="ObjectExtensions" /> class contains some useful methods to enable various tasks to be performed in a more fluid, English sentence, style</summary>
public static class ObjectExtensions
{
    private static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    extension<T>(T obj)
    {
        /// <summary>
        ///     The ToJson method, as you might expect, converts the supplied object to its JSON equivalent (using the
        ///     JsonSerializerDefaults.Web defaults with WriteIndented set to true)
        /// </summary>
        /// <returns>The JSON string of the object supplied</returns>
        public string ToJson() =>
            JsonSerializer.Serialize(obj, options);
    }
}
