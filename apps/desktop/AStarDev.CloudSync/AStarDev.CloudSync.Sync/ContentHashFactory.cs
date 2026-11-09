using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ContentHash"/> values.</summary>
public static class ContentHashFactory
{
    private const string MissingValueError = "A ContentHash needs a value.";

    /// <summary>Creates the value from the provider's text, trimmed.</summary>
    /// <param name="value">The provider's value.</param>
    /// <returns>The value, or an error when it is blank.</returns>
    public static Result<ContentHash, ErrorResponse> Create(string? value) => string.IsNullOrWhiteSpace(value)
        ? new Fail<ContentHash, ErrorResponse>(new ErrorResponse(MissingValueError))
        : new Ok<ContentHash, ErrorResponse>(new ContentHash(value.Trim()));
}
