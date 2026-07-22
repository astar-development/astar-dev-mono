namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Functional helpers and utilities for working with <see cref="Validation{T}" />, including the
///     applicative <see cref="Apply{T,TResult}" />/<see cref="Combine{T}" /> operations that accumulate
///     errors instead of stopping at the first failure.
/// </summary>
public static class ValidationExtensions
{
    private const string UnexpectedValidationTypeMessage = "Unexpected validation type.";

    /// <summary>
    ///     Applies a validated function to a validated value. When both sides are invalid, the errors from
    ///     both are accumulated (function errors first, then value errors) into a single <see cref="Invalid{T}" />.
    /// </summary>
    public static Validation<TResult> Apply<T, TResult>(this Validation<Func<T, TResult>> validationFunc, Validation<T> validationValue)
        => (validationFunc, validationValue) switch
            {
                (Valid<Func<T, TResult>> validFunc, Valid<T> validValue) => new Valid<TResult>(validFunc.Value(validValue.Value)),
                (Invalid<Func<T, TResult>> invalidFunc, Valid<T>) => new Invalid<TResult>(invalidFunc.Errors),
                (Valid<Func<T, TResult>>, Invalid<T> invalidValue) => new Invalid<TResult>(invalidValue.Errors),
                (Invalid<Func<T, TResult>> invalidFunc, Invalid<T> invalidValue) => new Invalid<TResult>([..invalidFunc.Errors, ..invalidValue.Errors]),
                _ => throw new InvalidOperationException(UnexpectedValidationTypeMessage)
            };

    /// <summary>
    ///     Combines a sequence of validations into a single <see cref="Validation{T}" /> of the ordered values.
    ///     When one or more validations are invalid, all of their errors are accumulated, in encounter order,
    ///     into a single <see cref="Invalid{T}" />.
    /// </summary>
    public static Validation<IReadOnlyList<T>> Combine<T>(this IEnumerable<Validation<T>> validations)
    {
        var values = new List<T>();
        var errors = new List<ValidationError>();

        foreach (var validation in validations)
        {
            switch (validation)
            {
                case Valid<T> valid:
                    values.Add(valid.Value);
                    break;

                case Invalid<T> invalid:
                    errors.AddRange(invalid.Errors);
                    break;

                default:
                    throw new InvalidOperationException(UnexpectedValidationTypeMessage);
            }
        }

        return errors.Count > 0
            ? new Invalid<IReadOnlyList<T>>(errors)
            : new Valid<IReadOnlyList<T>>(values);
    }

    /// <summary>
    ///     Reduces a <see cref="Validation{T}" /> to a single value by invoking <paramref name="onValid" />
    ///     with the validated value, or <paramref name="onInvalid" /> with the accumulated errors.
    /// </summary>
    public static TOut Match<T, TOut>(this Validation<T> validation, Func<T, TOut> onValid, Func<IReadOnlyList<ValidationError>, TOut> onInvalid)
        => validation switch
            {
                Valid<T> valid => onValid(valid.Value),
                Invalid<T> invalid => onInvalid(invalid.Errors),
                _ => throw new InvalidOperationException(UnexpectedValidationTypeMessage)
            };

    /// <summary>
    ///     Lifts a <see cref="Validation{T}" /> into a <see cref="Result{TResult,TError}" />, mapping the
    ///     accumulated errors to a domain error via <paramref name="mapErrors" />.
    /// </summary>
    public static Result<T, TError> ToResult<T, TError>(this Validation<T> validation, Func<IReadOnlyList<ValidationError>, TError> mapErrors)
        => validation switch
            {
                Valid<T> valid => new Ok<T, TError>(valid.Value),
                Invalid<T> invalid => new Fail<T, TError>(mapErrors(invalid.Errors)),
                _ => throw new InvalidOperationException(UnexpectedValidationTypeMessage)
            };
}
