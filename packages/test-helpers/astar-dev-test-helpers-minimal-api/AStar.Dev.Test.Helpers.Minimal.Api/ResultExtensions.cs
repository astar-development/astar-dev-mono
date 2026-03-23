using System.Reflection;

namespace AStar.Dev.Test.Helpers.Minimal.Api;

/// <summary>
///     The <see cref="ResultExtensions" /> class containing the relevant extensions on the <see cref="IResult" /> interface
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    ///     The GetResultValue method will return the value property from the <see cref="IResult" /> object
    ///     This method exists as the underlying classes are, for reasons best known to Microsoft, internal sealed classes...
    /// </summary>
    /// <param name="result">The <see cref="IResult" /> object to return the value from</param>
    /// <typeparam name="T">The type of the value to return</typeparam>
    /// <returns>A nullable instance of the specified return type</returns>
    public static T? ResultValue<T>(this IResult result) =>
        (T?)result.GetType()
                  .GetProperty("Value", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                  ?.GetValue(result);

    /// <summary>
    ///     The GetResultStatusCode method will return the Status Code property from the <see cref="IResult" /> object
    ///     This method exists as the underlying classes are, for reasons best known to Microsoft, internal sealed classes...
    /// </summary>
    /// <param name="result">The <see cref="IResult" /> object to return the Status Code from</param>
    /// <returns>A nullable instance of the specified return type</returns>
    public static int? ResultStatusCode(this IResult result) =>
        (int?)result.GetType()
                    .GetProperty("StatusCode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?.GetValue(result);
}
