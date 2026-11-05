using AStarDev.CloudSync.Core.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="ProviderAccountKey"/> values.</summary>
public static class ProviderAccountKeyFactory
{
    /// <summary>Creates a key from the provider's account identifier. The value is trimmed and lower-cased, so keys that differ only by case are equal. Returns a failure when the value is null, empty or whitespace.</summary>
    /// <param name="value">The provider's account identifier.</param>
    public static Result<ProviderAccountKey, ErrorResponse> Create(string? value) => throw new NotImplementedException();
}
