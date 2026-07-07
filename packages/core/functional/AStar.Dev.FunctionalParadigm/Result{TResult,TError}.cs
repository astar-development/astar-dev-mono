namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Represents the outcome of an operation that may either succeed with a
///     <typeparamref name="TResult" /> or fail with a <typeparamref name="TError" />.
///     Use the <see cref="Result" /> factory class to construct instances.
/// </summary>
/// <typeparam name="TResult">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the failure error.</typeparam>
public abstract record Result<TResult, TError>
{
    /// <summary>
    ///     Restricts derivation of <see cref="Result{TResult,TError}" /> to <see cref="Ok{TResult,TError}" /> and
    ///     <see cref="Fail{TResult,TError}" />, both declared in this assembly.
    /// </summary>
    private protected Result()
    {
    }

    /// <summary>
    ///     Implicitly lifts a success value into a <see cref="Result{TResult,TError}" />.
    /// </summary>
    public static implicit operator Result<TResult, TError>(TResult value) => new Ok<TResult, TError>(value);

    /// <summary>
    ///     Implicitly lifts an error into a <see cref="Result{TResult,TError}" />.
    /// </summary>
    public static implicit operator Result<TResult, TError>(TError error) => new Fail<TResult, TError>(error);
}

/// <summary>
///     Represents a successful <see cref="Result{TResult,TError}" /> carrying a value.
/// </summary>
public record Ok<TResult, TError>(TResult Value) : Result<TResult, TError>;

/// <summary>
///     Represents a failed <see cref="Result{TResult,TError}" /> carrying an error.
/// </summary>
public record Fail<TResult, TError>(TError Error) : Result<TResult, TError>;
