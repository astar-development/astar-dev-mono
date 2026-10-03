namespace AStarDev.FunctionalParadigm;

/// <summary>
///     Functional helpers and utilities for working with <see cref="Validation{T}" />, including the
///     applicative <see cref="Apply{T,TResult}" />/<see cref="Combine{T}" /> operations that accumulate
///     errors instead of stopping at the first failure.
/// </summary>
public static class ValidationExtensions
{
    private const string UnexpectedValidationTypeMessage = "Unexpected validation type.";

    extension<T, TResult>(Validation<Func<T, TResult>> validationFunc)
    {
        /// <summary>
        ///     Applies a validated function to a validated value. When both sides are invalid, the errors from
        ///     both are accumulated (function errors first, then value errors) into a single <see cref="Invalid{T}" />.
        /// </summary>
        public Validation<TResult> Apply(Validation<T> validationValue)
            => (validationFunc, validationValue) switch
            {
                (Valid<Func<T, TResult>> validFunc, Valid<T> validValue) => new Valid<TResult>(validFunc.Value(validValue.Value)),
                (Invalid<Func<T, TResult>> invalidFunc, Valid<T>) => new Invalid<TResult>(invalidFunc.Errors),
                (Valid<Func<T, TResult>>, Invalid<T> invalidValue) => new Invalid<TResult>(invalidValue.Errors),
                (Invalid<Func<T, TResult>> invalidFunc, Invalid<T> invalidValue) => new Invalid<TResult>([.. invalidFunc.Errors, .. invalidValue.Errors]),
                _ => throw new InvalidOperationException(UnexpectedValidationTypeMessage)
            };
    }

    extension<T>(IEnumerable<Validation<T>> validations)
    {
        /// <summary>
        ///     Combines a sequence of validations into a single <see cref="Validation{T}" /> of the ordered values.
        ///     When one or more validations are invalid, all of their errors are accumulated, in encounter order,
        ///     into a single <see cref="Invalid{T}" />.
        /// </summary>
        public Validation<IReadOnlyList<T>> Combine()
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
    }

    extension<T>(Validation<T> validation)
    {
        /// <summary>
        ///     Reduces a <see cref="Validation{T}" /> to a single value by invoking <paramref name="onValid" />
        ///     with the validated value, or <paramref name="onInvalid" /> with the accumulated errors.
        /// </summary>
        public TOut Match<TOut>(Func<T, TOut> onValid, Func<IReadOnlyList<ValidationError>, TOut> onInvalid)
            => validation switch
            {
                Valid<T> valid => onValid(valid.Value),
                Invalid<T> invalid => onInvalid(invalid.Errors),
                _ => throw new InvalidOperationException(UnexpectedValidationTypeMessage)
            };

        /// <summary>Attempts to extract the value from a <see cref="Validation{T}" />.</summary>
        public bool TryGetValue(out T value)
        {
            if (validation is Valid<T> valid)
            {
                value = valid.Value;

                return true;
            }

            value = default!;

            return false;
        }

        /// <summary>Attempts to extract the accumulated errors from a <see cref="Validation{T}" />.</summary>
        public bool TryGetErrors(out IReadOnlyList<ValidationError> errors)
        {
            if (validation is Invalid<T> invalid)
            {
                errors = invalid.Errors;

                return true;
            }

            errors = [];

            return false;
        }

        /// <summary>Lifts a <see cref="Validation{T}" /> into a <see cref="Result{TResult,TError}" />, mapping the accumulated errors to a domain error via <paramref name="mapErrors" />.</summary>
        public Result<T, TError> ToResult<TError>(Func<IReadOnlyList<ValidationError>, TError> mapErrors)
            => validation switch
            {
                Valid<T> valid => new Ok<T, TError>(valid.Value),
                Invalid<T> invalid => new Fail<T, TError>(mapErrors(invalid.Errors)),
                _ => throw new InvalidOperationException(UnexpectedValidationTypeMessage)
            };
    }
}
