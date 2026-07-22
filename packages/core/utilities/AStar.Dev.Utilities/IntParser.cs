namespace AStar.Dev.Utilities;

/// <summary>
/// 
/// </summary>
public static class IntParser
{
    /// <summary>
    ///     The ToInt method will parse the supplied string and return the matching int value
    /// </summary>
    /// <param name="value">The value to parse to the int</param>
    /// <returns>The parsed value as the matching int value</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid int value</exception>
    public static int ToInt(this string value) =>
        int.Parse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    ///    The ToIntSafe method will parse the supplied string and return the matching int value, or 0 if the string is not a valid int value
    /// </summary>
    /// <param name="value">The value to parse to the int</param>
    /// <returns>The parsed value as the matching int value, or 0 if the string is not a valid int value</returns>
    public static int ToIntSafe(this string value) =>
        int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int result) ? result : 0;
}