using System.Text.Json;

namespace AStar.Dev.Utilities;

/// <summary>
///     The <see href="Constants"></see>see> class contains static / constant properties to simplify and centralise various
///     settings
/// </summary>
public static class Constants
{
    /// <summary>
    ///     Returns an instance of <see href="JsonSerializerOptions"></see> configured with the Web defaults
    /// </summary>
    public static JsonSerializerOptions WebDeserialisationSettings => new(JsonSerializerDefaults.Web);
}