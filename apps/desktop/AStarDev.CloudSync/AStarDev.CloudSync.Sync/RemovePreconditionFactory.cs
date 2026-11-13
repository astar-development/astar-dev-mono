using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemovePrecondition"/> cases.</summary>
public static class RemovePreconditionFactory
{
    /// <summary>Creates the <see cref="RemovePrecondition.MustMatch"/> case.</summary>
    /// <returns>The precondition, or an error when the tag is blank (an empty If-Match would fail every request).</returns>
    public static Result<RemovePrecondition, ErrorResponse> CreateMustMatch(EntityTag eTag) => string.IsNullOrWhiteSpace(eTag.Value)
        ? new Fail<RemovePrecondition, ErrorResponse>(new ErrorResponse("A RemovePrecondition needs the tag last seen."))
        : new Ok<RemovePrecondition, ErrorResponse>(new RemovePrecondition.MustMatch(eTag));

    /// <summary>Creates the <see cref="RemovePrecondition.MustMatchContent"/> case.</summary>
    /// <returns>The precondition, or an error when the tag is blank (an empty If-Match would fail every request).</returns>
    public static Result<RemovePrecondition, ErrorResponse> CreateMustMatchContent(ContentTag cTag) => string.IsNullOrWhiteSpace(cTag.Value)
        ? new Fail<RemovePrecondition, ErrorResponse>(new ErrorResponse("A RemovePrecondition needs the tag last seen."))
        : new Ok<RemovePrecondition, ErrorResponse>(new RemovePrecondition.MustMatchContent(cTag));
}
