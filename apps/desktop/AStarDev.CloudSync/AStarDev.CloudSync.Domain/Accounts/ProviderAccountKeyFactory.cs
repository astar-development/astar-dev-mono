using AStarDev.CloudSync.Core.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="ProviderAccountKey"/> values.</summary>
public static class ProviderAccountKeyFactory
{
    private const string MissingValueError = "The provider account key must not be null, empty or whitespace.";

    /// <summary>Creates a key from the provider's account identifier. The value is trimmed and keeps its case, so it still matches the provider's identifier exactly. Returns a failure when the value is null, empty or whitespace.</summary>
    /// <param name="value">The provider's account identifier.</param>
    public static Result<ProviderAccountKey, ErrorResponse> Create(string? value) => string.IsNullOrWhiteSpace(value)
        ? new Fail<ProviderAccountKey, ErrorResponse>(new ErrorResponse(MissingValueError))
        : new Ok<ProviderAccountKey, ErrorResponse>(new ProviderAccountKey(value.Trim()));
}
