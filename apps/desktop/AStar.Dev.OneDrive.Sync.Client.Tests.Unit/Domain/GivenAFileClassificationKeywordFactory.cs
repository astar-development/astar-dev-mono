using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAFileClassificationKeywordFactory
{
    private const string AnyValidValue = "Cats";

    [Fact]
    public void when_value_is_empty_then_result_is_failure()
    {
        var result = FileClassificationKeywordFactory.Create("", Option.None<bool>());

        _ = result.ShouldBeOfType<Result<FileClassificationKeyword, string>.Error>();
    }

    [Fact]
    public void when_value_is_whitespace_then_result_is_failure()
    {
        var result = FileClassificationKeywordFactory.Create("   ", Option.None<bool>());

        _ = result.ShouldBeOfType<Result<FileClassificationKeyword, string>.Error>();
    }

    [Fact]
    public void when_value_has_leading_and_trailing_spaces_then_value_is_trimmed()
    {
        var result = FileClassificationKeywordFactory.Create("  cats  ", Option.None<bool>());

        result.Match(k => k.Value, _ => string.Empty).ShouldBe(AnyValidValue);
    }

    [Fact]
    public void when_value_has_uppercase_then_value_is_lowercased()
    {
        var result = FileClassificationKeywordFactory.Create("CATS", Option.None<bool>());

        result.Match(k => k.Value, _ => string.Empty).ShouldBe(AnyValidValue);
    }

    [Fact]
    public void when_value_is_valid_then_result_is_success()
    {
        var result = FileClassificationKeywordFactory.Create(AnyValidValue, Option.None<bool>());

        _ = result.ShouldBeOfType<Result<FileClassificationKeyword, string>.Ok>();
    }

    [Fact]
    public void when_value_is_valid_with_special_override_true_then_result_is_success()
    {
        var result = FileClassificationKeywordFactory.Create(AnyValidValue, Option.Some(true));

        result.Match(k => k.IsSpecialOverride, _ => Option.None<bool>()).ShouldBe(Option.Some(true));
    }

    [Fact]
    public void when_value_is_valid_with_special_override_false_then_result_is_success()
    {
        var result = FileClassificationKeywordFactory.Create(AnyValidValue, Option.Some(false));

        result.Match(k => k.IsSpecialOverride, _ => Option.None<bool>()).ShouldBe(Option.Some(false));
    }

    [Fact]
    public void when_value_is_valid_with_no_override_then_is_special_override_is_none()
    {
        var result = FileClassificationKeywordFactory.Create(AnyValidValue, Option.None<bool>());

        result.Match(k => k.IsSpecialOverride, _ => Option.Some(true)).ShouldBe(Option.None<bool>());
    }

    [Fact]
    public void when_value_is_null_then_result_is_failure()
    {
        var result = FileClassificationKeywordFactory.Create(null!, Option.None<bool>());

        _ = result.ShouldBeOfType<Result<FileClassificationKeyword, string>.Error>();
    }

    [Fact]
    public void when_value_has_mixed_case_and_surrounding_spaces_then_value_is_trimmed_and_lowercased()
    {
        var result = FileClassificationKeywordFactory.Create("  CaTs  ", Option.None<bool>());

        result.Match(k => k.Value, _ => string.Empty).ShouldBe(AnyValidValue);
    }
}
