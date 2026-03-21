namespace AStar.Dev.Utilities;

/// <summary>
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    ///     The ParseEnum method will parse the supplied string and return the matching enum value
    /// </summary>
    /// <typeparam name="T">The typeof of the expected enum</typeparam>
    /// <param name="value">The value to parse to the enum</param>
    /// <returns>The parsed value as the matching enum value</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid enum value</exception>
    public static T ParseEnum<T>(this string value) =>
        (T)Enum.Parse(typeof(T), value, true);
}