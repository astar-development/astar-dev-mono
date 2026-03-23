using System.Numerics;

namespace AStar.Dev.FluentAssignments;

/// <summary>
/// The <see cref="FluentAssignmentsExtensions"/> class contains the initial extension methods. Over time, this may be separated into separate classes.
/// </summary>
public static class FluentAssignmentsExtensions
{
    /// <summary>
    /// The <see cref="WillBeSet{T}"/> method provides a simple pass-through starting point for the assignment.
    /// There is no functionality in this method, it exists purely to make the sentence more fluent / readable.
    /// </summary>
    /// <typeparam name="T">The typeof the parameter for the assignment.</typeparam>
    /// <param name="value">The actual value that may be assigned.</param>
    /// <returns>The value supplied to the extension method.</returns>
    public static T WillBeSet<T>(this T value) where T : INumber<T> => value;

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T IfItIs<T>(this T value) where T : INumber<T> => value;

    /// <summary>
    /// This method will check whether the specified object is null or not.
    /// </summary>
    /// <typeparam name="T">
    /// Specifies the generic object to check for null.
    /// </typeparam>
    /// <param name="object">
    /// The object to check for null.
    /// </param>
    /// <returns>
    /// The original object if it is not null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the object is, in fact, null.
    /// </exception>
    public static T NotNull<T>(this T @object) => @object is null ? throw new ArgumentNullException(nameof(@object)) : @object;

    /// <summary>
    /// Here just to make the checks more fluent...
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T And<T>(this T value) where T : INumber<T> => value;

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="minimum"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static T ItIsGreaterThan<T>(this T value, T minimum) where T : INumber<T> => value <= minimum ? throw new ArgumentException($"The specified value of {value} was not greater than the specified minimum of {minimum}", nameof(minimum)) : value;

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="maximum"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static T ItIsLessThan<T>(this T value, T maximum) where T : INumber<T> => value >= maximum ? throw new ArgumentException($"The specified value of {value} was not less than the specified maximum of {maximum}", nameof(maximum)) : value;
}
