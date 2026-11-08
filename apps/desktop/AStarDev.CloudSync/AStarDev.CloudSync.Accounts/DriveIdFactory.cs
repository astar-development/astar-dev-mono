using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts;

/// <summary>Creates <see cref="DriveId"/> values.</summary>
public static class DriveIdFactory
{
    private const string MissingValueError = "The drive id must not be null, empty or whitespace.";

    /// <summary>Creates a drive id. The value is trimmed and keeps its case. Returns a failure when the value is null, empty or whitespace.</summary>
    /// <param name="value">The provider's drive identifier.</param>
    public static Result<DriveId, ErrorResponse> Create(string? value) => string.IsNullOrWhiteSpace(value)
        ? new Fail<DriveId, ErrorResponse>(new ErrorResponse(MissingValueError))
        : new Ok<DriveId, ErrorResponse>(new DriveId(value.Trim()));
}
