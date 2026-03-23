namespace AStar.Dev.Infrastructure.Data;

/// <summary>
///     The <see href="ConnectionString"></see> class for use when configuring the context
/// </summary>
public sealed class ConnectionString
{
    /// <summary>
    ///     Gets or sets the actual connection string
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    ///     The implicit convertor from <see cref="ConnectionString" /> to a simple <see cref="string" />
    /// </summary>
    /// <param name="connectionString">The instance of <see cref="ConnectionString" /> to convert.</param>
    /// <returns>The simple string representation</returns>
    public static implicit operator string(ConnectionString connectionString) =>
        connectionString.Value;

    /// <summary>
    ///     The implicit convertor from a simple <see cref="string" /> to a <see cref="ConnectionString" />
    /// </summary>
    /// <param name="connectionString">The instance of <see cref="string" /> to convert.</param>
    /// <returns>The converted representation</returns>
    public static implicit operator ConnectionString(string connectionString) =>
        new() { Value = connectionString };
}
