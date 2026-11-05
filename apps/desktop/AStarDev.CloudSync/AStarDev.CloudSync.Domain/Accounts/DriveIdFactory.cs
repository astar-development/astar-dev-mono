using AStarDev.CloudSync.Core.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="DriveId"/> values.</summary>
public static class DriveIdFactory
{
    /// <summary>Creates a drive id. The value is trimmed and keeps its case. Returns a failure when the value is null, empty or whitespace.</summary>
    /// <param name="value">The provider's drive identifier.</param>
    public static Result<DriveId, ErrorResponse> Create(string? value) => throw new NotImplementedException();
}
