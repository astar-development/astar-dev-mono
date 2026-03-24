using System.Numerics;

namespace AStar.Dev.Guard.Clauses;

/// <summary>
///     The root <seealso href="GuardAgainst"></seealso> class.
/// </summary>
public static class GuardAgainst
{
    /// <summary>
    ///     This method will check whether the specified object is null or not.
    /// </summary>
    /// <typeparam name="T">
    ///     Specifies the generic object to check for null.
    /// </typeparam>
    /// <param name="objectToCheck">
    ///     The object to check for null.
    /// </param>
    /// <returns>
    ///     The original object if it is not null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the object is, in fact, null.
    /// </exception>
    public static T Null<T>(T objectToCheck)
    {
        return objectToCheck is null ? throw new ArgumentNullException(nameof(objectToCheck)) : objectToCheck;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="objectToCheck"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static INumber<T> Negative<T>(T objectToCheck) where T : INumber<T>
    {
        return objectToCheck < T.Zero ? throw new ArgumentOutOfRangeException(nameof(objectToCheck)) : objectToCheck;
    }
}
