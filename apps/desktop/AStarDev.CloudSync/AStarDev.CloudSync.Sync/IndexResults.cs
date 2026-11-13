using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

internal static class IndexResults
{
    internal static T OrThrow<T>(this Result<T, ErrorResponse> result) => result.Match(value => value, error => throw new InvalidOperationException(error.Message));
}
