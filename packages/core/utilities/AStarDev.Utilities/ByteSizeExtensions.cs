namespace AStarDev.Utilities;

/// <summary>Provides extension methods for formatting byte counts.</summary>
public static class ByteSizeExtensions
{
    private static readonly string[] units = ["B", "KB", "MB", "GB", "TB"];

    extension(int bytes)
    {
        /// <summary>Formats the specified byte count using whichever unit reads best for its magnitude.</summary>
        /// <returns>A human-readable size, e.g. "512 B", "42 KB", or "3.1 MB".</returns>
        public string ToFileSizeString()
        {
            if (bytes <= 0) return "0 B";

            var unitIndex = 0;
            var size = (double)bytes;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{Math.Round(size, unitIndex == 0 ? 0 : 1):0.#} {units[unitIndex]}";
        }
    }
}
