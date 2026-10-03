namespace AStarDev.Utilities;

/// <summary>
/// </summary>
public static class EnumExtensions
{
    extension(string value)
    {
        /// <summary>The ParseEnum method will parse the supplied string and return the matching enum value</summary>
        /// <typeparam name="T">The typeof of the expected enum</typeparam>
        /// <returns>The parsed value as the matching enum value</returns>
        /// <exception cref="ArgumentException">Thrown when the string is not a valid enum value</exception>
        public T ParseEnum<T>() =>
            (T)Enum.Parse(typeof(T), value, true);
    }
}
