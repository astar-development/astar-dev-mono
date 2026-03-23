namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Represents an error response object containing a message describing the error.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// </summary>
    /// <param name="message"></param>
    public ErrorResponse(string message) => Message = message;

    /// <summary>
    ///     Represents the message associated with an error response.
    /// </summary>
    public string Message { get; }
}
