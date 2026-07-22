namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenValidationMatch
{
    [Fact]
    public void when_validation_is_valid_then_the_valid_branch_is_invoked_with_the_value()
    {
        var validation = Validation.Valid(9);

        int actual = validation.Match(value => value * 2, _ => -1);

        actual.ShouldBe(18);
    }

    [Fact]
    public void when_validation_is_invalid_then_the_invalid_branch_is_invoked_with_all_errors()
    {
        var errors = new List<ValidationError>
        {
            ValidationErrorFactory.Create("Name", "required"),
            ValidationErrorFactory.Create("Age", "must be positive")
        };
        var validation = Validation.Invalid<int>(errors);

        string actual = validation.Match(_ => "valid", invalidErrors => string.Join(",", invalidErrors.Select(error => error.Message)));

        actual.ShouldBe("required,must be positive");
    }

    [Fact]
    public void when_validation_is_valid_then_the_invalid_branch_is_never_invoked()
    {
        var validation = Validation.Valid("value");
        int invalidBranchInvocationCount = 0;

        _ = validation.Match(value => value, _ =>
        {
            invalidBranchInvocationCount++;

            return "invalid";
        });

        invalidBranchInvocationCount.ShouldBe(0);
    }

    [Fact]
    public void when_validation_is_invalid_then_the_valid_branch_is_never_invoked()
    {
        var validation = Validation.Invalid<string>(ValidationErrorFactory.Create("Name", "required"));
        int validBranchInvocationCount = 0;

        _ = validation.Match(value =>
        {
            validBranchInvocationCount++;

            return value;
        }, _ => "invalid");

        validBranchInvocationCount.ShouldBe(0);
    }
}
