using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ChangeCursor"/> values.</summary>
public static class ChangeCursorFactory
{
    private const string MissingValueError = "A ChangeCursor needs a value.";

    /// <summary>Creates the value from the provider's text, trimmed.</summary>
    /// <param name="value">The provider's value.</param>
    /// <returns>The value, or an error when it is blank.</returns>
    public static Result<ChangeCursor, ErrorResponse> Create(string? value) => string.IsNullOrWhiteSpace(value)
        ? new Fail<ChangeCursor, ErrorResponse>(new ErrorResponse(MissingValueError))
        : new Ok<ChangeCursor, ErrorResponse>(new ChangeCursor(value.Trim()));
}
