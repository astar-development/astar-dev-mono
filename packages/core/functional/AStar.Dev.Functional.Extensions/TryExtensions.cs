namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Extensions for the Try class to convert Result types.
///     These extensions allow for converting Result types with exceptions to Result types with ErrorResponse.
/// </summary>
public static class TryExtensions
{
    /// <summary>
    ///     Converts a Result with an Exception to a Result with an ErrorResponse (specifically, the base exception message is mapped to the ErrorResponse - please note: NO translation happens...).
    /// </summary>
    /// <typeparam name="T">The type of the successful result</typeparam>
    /// <param name="result">The Result object being extended.</param>
    /// <returns>A success result without change if applicable, otherwise, the exception will be mapped to an ErrorResponse</returns>
    public static Result<T, ErrorResponse> ToErrorResponse<T>(this Result<T, Exception> result)
        => result.MapFailure(ex => new ErrorResponse(ex.GetBaseException().Message));
}
