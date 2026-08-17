namespace AStar.Dev.Technical.Debt.Reporting;

/// <summary>
///     The <see cref="RefactorAttribute" /> attribute
/// </summary>
/// <param name="painEstimate">The estimated hours to resolve</param>
/// <param name="hoursToResolve">The estimated pain level</param>
/// <param name="description">The description of the refactoring to be performed</param>
[AttributeUsage(AttributeTargets.All, Inherited = false)]
public sealed class RefactorAttribute(int painEstimate, int hoursToResolve, string description) : Attribute
{
    /// <summary>
    ///     The estimated hours to resolve
    /// </summary>
    public int HoursToResolve { get; } = hoursToResolve;

    /// <summary>
    ///     Gets the description of the refactoring to be performed
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    ///     The estimated pain level - quantity to be defined
    /// </summary>
    public int PainEstimate => painEstimate;
}
