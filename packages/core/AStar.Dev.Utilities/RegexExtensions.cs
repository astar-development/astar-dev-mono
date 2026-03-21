using System.Text.RegularExpressions;

namespace AStar.Dev.Utilities;

/// <summary>
/// </summary>
public static partial class RegexExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool ContainsAtLeastOneLowercaseLetter(this string value) =>
        LowercaseLettersRegex().IsMatch(value);

    /// <summary>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool ContainsAtLeastOneUppercaseLetter(this string value) =>
        UppercaseLettersRegex().IsMatch(value);

    /// <summary>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool ContainsAtLeastOneDigit(this string value) =>
        DigitRegex().IsMatch(value);

    /// <summary>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool ContainsAtLeastOneSpecialCharacter(this string value) =>
        SpecialCharacterRegex().IsMatch(value);

    [GeneratedRegex("[a-z]", RegexOptions.CultureInvariant, 1_000)]
    private static partial Regex LowercaseLettersRegex();

    [GeneratedRegex("[A-Z]", RegexOptions.CultureInvariant, 1_000)]
    private static partial Regex UppercaseLettersRegex();

    [GeneratedRegex("[0-9]", RegexOptions.CultureInvariant, 1_000)]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"[!-\/:-@[-`¬{-~]", RegexOptions.CultureInvariant, 1_000)]
    private static partial Regex SpecialCharacterRegex();
}