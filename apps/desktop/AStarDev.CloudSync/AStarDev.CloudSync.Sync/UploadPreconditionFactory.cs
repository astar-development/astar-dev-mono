using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadPrecondition"/> cases.</summary>
public static class UploadPreconditionFactory
{
    /// <summary>Creates the <see cref="UploadPrecondition.MustNotExist"/> case.</summary>
    public static UploadPrecondition CreateMustNotExist() => new UploadPrecondition.MustNotExist();

    /// <summary>Creates the <see cref="UploadPrecondition.MustMatch"/> case.</summary>
    /// <returns>The precondition, or an error when the tag is blank (an empty If-Match would fail every request).</returns>
    public static Result<UploadPrecondition, ErrorResponse> CreateMustMatch(ContentTag cTag) => string.IsNullOrWhiteSpace(cTag.Value)
        ? new Fail<UploadPrecondition, ErrorResponse>(new ErrorResponse("A UploadPrecondition needs the tag last seen."))
        : new Ok<UploadPrecondition, ErrorResponse>(new UploadPrecondition.MustMatch(cTag));
}
