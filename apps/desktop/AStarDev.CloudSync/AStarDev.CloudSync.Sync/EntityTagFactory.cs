using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="EntityTag"/> values.</summary>
public static class EntityTagFactory
{
    private const string MissingValueError = "A EntityTag needs a value.";

    /// <summary>Creates the value from the provider's text, trimmed.</summary>
    /// <param name="value">The provider's value.</param>
    /// <returns>The value, or an error when it is blank.</returns>
    public static Result<EntityTag, ErrorResponse> Create(string? value) => throw new NotImplementedException($"{value}");
}
