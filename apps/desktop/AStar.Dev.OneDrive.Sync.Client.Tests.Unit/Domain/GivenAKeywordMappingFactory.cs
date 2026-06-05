using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAKeywordMappingFactory
{
    private const string AnyKeyword = "holiday";
    private const string AnyLevel1 = "Memories";

    [Fact]
    public void when_valid_inputs_then_result_is_success()
    {
        var result = KeywordMappingFactory.Create(AnyKeyword, AnyLevel1, Option.None<string>(), Option.None<string>(), false);

        _ = result.ShouldBeOfType<Result<KeywordMapping, string>.Ok>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_keyword_is_empty_then_result_is_error(string blankKeyword)
    {
        var result = KeywordMappingFactory.Create(blankKeyword, AnyLevel1, Option.None<string>(), Option.None<string>(), false);

        _ = result.ShouldBeOfType<Result<KeywordMapping, string>.Error>();
    }

    [Fact]
    public void when_keyword_is_whitespace_then_result_is_error()
    {
        var result = KeywordMappingFactory.Create("   ", AnyLevel1, Option.None<string>(), Option.None<string>(), false);

        _ = result.ShouldBeOfType<Result<KeywordMapping, string>.Error>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_level1_is_empty_then_result_is_error(string blankLevel1)
    {
        var result = KeywordMappingFactory.Create(AnyKeyword, blankLevel1, Option.None<string>(), Option.None<string>(), false);

        _ = result.ShouldBeOfType<Result<KeywordMapping, string>.Error>();
    }

    [Fact]
    public void when_valid_inputs_then_all_properties_set_correctly()
    {
        var level2 = Option.Some("Holidays");
        var level3 = Option.Some("Beach");

        var result = KeywordMappingFactory.Create(AnyKeyword, AnyLevel1, level2, level3, true);

        var mapping = result.Match(m => m, _ => null!);
        mapping.Keyword.ShouldBe(AnyKeyword);
        mapping.Level1.ShouldBe(AnyLevel1);
        mapping.Level2.ShouldBe(level2);
        mapping.Level3.ShouldBe(level3);
        mapping.IsSpecial.ShouldBeTrue();
    }

    [Fact]
    public void when_keyword_has_whitespace_then_keyword_is_trimmed()
    {
        var result = KeywordMappingFactory.Create("  holiday  ", AnyLevel1, Option.None<string>(), Option.None<string>(), false);

        result.Match(m => m.Keyword, _ => string.Empty).ShouldBe("holiday");
    }

    [Fact]
    public void when_keyword_is_null_then_result_is_error()
    {
        var result = KeywordMappingFactory.Create(null!, AnyLevel1, Option.None<string>(), Option.None<string>(), false);

        _ = result.ShouldBeOfType<Result<KeywordMapping, string>.Error>();
    }

    [Fact]
    public void when_level1_is_null_then_result_is_error()
    {
        var result = KeywordMappingFactory.Create(AnyKeyword, null!, Option.None<string>(), Option.None<string>(), false);

        _ = result.ShouldBeOfType<Result<KeywordMapping, string>.Error>();
    }
}
