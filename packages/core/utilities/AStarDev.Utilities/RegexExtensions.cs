using System.Text.RegularExpressions;

namespace AStarDev.Utilities;

/// <summary>
/// </summary>
public static partial class RegexExtensions
{
    extension(string value)
    {
        /// <summary>Determines whether the string contains at least one lowercase letter.</summary>
        /// <returns><c>true</c> if the string contains at least one lowercase letter; otherwise, <c>false</c>.</returns>
        public bool ContainsAtLeastOneLowercaseLetter() =>
            LowercaseLettersRegex().IsMatch(value);

        /// <summary>Determines whether the string contains at least one uppercase letter.</summary>
        /// <returns><c>true</c> if the string contains at least one uppercase letter; otherwise, <c>false</c>.</returns>
        public bool ContainsAtLeastOneUppercaseLetter() =>
            UppercaseLettersRegex().IsMatch(value);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool ContainsAtLeastOneDigit() =>
            DigitRegex().IsMatch(value);

        /// <summary>Determines whether the string contains at least one digit.</summary>
        /// <returns><c>true</c> if the string contains at least one digit; otherwise, <c>false</c>.</returns>
        public bool ContainsAtLeastOneSpecialCharacter() =>
            SpecialCharacterRegex().IsMatch(value);
    }

    [GeneratedRegex("[a-z]", RegexOptions.CultureInvariant, 1_000)]
    private static partial Regex LowercaseLettersRegex();

    [GeneratedRegex("[A-Z]", RegexOptions.CultureInvariant, 1_000)]
    private static partial Regex UppercaseLettersRegex();

    [GeneratedRegex("[0-9]", RegexOptions.CultureInvariant, 1_000)]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"[!-\/:-@[-`¬{-~]", RegexOptions.CultureInvariant, 1_000)]
    private static partial Regex SpecialCharacterRegex();
}
