using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ContentTag"/> values.</summary>
public static class ContentTagFactory
{
    private const string MissingValueError = "A ContentTag needs a value.";

    /// <summary>Creates the value from the provider's text, trimmed.</summary>
    /// <param name="value">The provider's value.</param>
    /// <returns>The value, or an error when it is blank.</returns>
    public static Result<ContentTag, ErrorResponse> Create(string? value) => string.IsNullOrWhiteSpace(value)
        ? new Fail<ContentTag, ErrorResponse>(new ErrorResponse(MissingValueError))
        : new Ok<ContentTag, ErrorResponse>(new ContentTag(value.Trim()));
}
