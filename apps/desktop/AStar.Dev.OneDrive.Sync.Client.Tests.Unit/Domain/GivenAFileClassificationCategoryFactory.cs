using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAFileClassificationCategoryFactory
{
    private const int AnyId = 42;
    private const string AnyValidName = "Vehicles";
    private static readonly FileClassificationCategoryId AnyParentCategoryId = new(7);

    [Fact]
    public void when_level1_created_with_valid_name_and_no_parent_then_result_is_success()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 1, Option.None<FileClassificationCategoryId>());

        _ = result.ShouldBeOfType<Result<FileClassificationCategory, string>.Ok>();
    }

    [Fact]
    public void when_level1_created_with_valid_name_and_no_parent_then_id_is_set_correctly()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 1, Option.None<FileClassificationCategoryId>());

        result.Match(c => c.Id, _ => new FileClassificationCategoryId(0)).ShouldBe(new FileClassificationCategoryId(AnyId));
    }

    [Fact]
    public void when_level1_created_with_valid_name_and_no_parent_then_name_is_set_correctly()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 1, Option.None<FileClassificationCategoryId>());

        result.Match(c => c.Name, _ => string.Empty).ShouldBe(AnyValidName);
    }

    [Fact]
    public void when_level1_created_with_valid_name_and_no_parent_then_level_is_set_correctly()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 1, Option.None<FileClassificationCategoryId>());

        result.Match(c => c.Level, _ => 0).ShouldBe(1);
    }

    [Fact]
    public void when_level1_created_with_valid_name_and_no_parent_then_parent_id_is_none()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 1, Option.None<FileClassificationCategoryId>());

        result.Match(c => c.ParentId, _ => Option.Some(new FileClassificationCategoryId(999))).ShouldBe(Option.None<FileClassificationCategoryId>());
    }

    [Fact]
    public void when_level2_created_with_valid_name_and_parent_then_result_is_success()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 2, Option.Some(AnyParentCategoryId));

        _ = result.ShouldBeOfType<Result<FileClassificationCategory, string>.Ok>();
    }

    [Fact]
    public void when_level3_created_with_valid_name_and_parent_then_result_is_success()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 3, Option.Some(AnyParentCategoryId));

        _ = result.ShouldBeOfType<Result<FileClassificationCategory, string>.Ok>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void when_level_is_out_of_range_then_result_is_failure(int invalidLevel)
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, invalidLevel, Option.None<FileClassificationCategoryId>());

        _ = result.ShouldBeOfType<Result<FileClassificationCategory, string>.Error>();
    }

    [Fact]
    public void when_level1_created_with_parent_supplied_then_result_is_failure()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 1, Option.Some(AnyParentCategoryId));

        _ = result.ShouldBeOfType<Result<FileClassificationCategory, string>.Error>();
    }

    [Fact]
    public void when_level2_created_with_no_parent_then_result_is_failure()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 2, Option.None<FileClassificationCategoryId>());

        _ = result.ShouldBeOfType<Result<FileClassificationCategory, string>.Error>();
    }

    [Fact]
    public void when_level3_created_with_no_parent_then_result_is_failure()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), AnyValidName, 3, Option.None<FileClassificationCategoryId>());

        _ = result.ShouldBeOfType<Result<FileClassificationCategory, string>.Error>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_name_is_blank_then_result_is_failure(string blankName)
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), blankName, 1, Option.None<FileClassificationCategoryId>());

        _ = result.ShouldBeOfType<Result<FileClassificationCategory, string>.Error>();
    }

    [Fact]
    public void when_name_has_leading_and_trailing_spaces_then_name_is_trimmed_in_result()
    {
        var result = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(AnyId), "  Vehicles  ", 1, Option.None<FileClassificationCategoryId>());

        result.Match(c => c.Name, _ => string.Empty).ShouldBe("Vehicles");
    }
}
