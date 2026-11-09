using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteItemId"/> values.</summary>
public static class RemoteItemIdFactory
{
    private const string MissingValueError = "A RemoteItemId needs a value.";

    /// <summary>Creates the value from the provider's text, trimmed.</summary>
    /// <param name="value">The provider's value.</param>
    /// <returns>The value, or an error when it is blank.</returns>
    public static Result<RemoteItemId, ErrorResponse> Create(string? value) => throw new NotImplementedException($"{value}");
}
