using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemovePrecondition"/> cases.</summary>
public static class RemovePreconditionFactory
{
    /// <summary>Creates the <see cref="RemovePrecondition.MustMatch"/> case.</summary>
    /// <returns>The precondition, or an error when the tag is blank (an empty If-Match would fail every request).</returns>
    public static Result<RemovePrecondition, ErrorResponse> CreateMustMatch(EntityTag eTag) => throw new NotImplementedException($"{eTag}");
}
