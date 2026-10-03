using AStarDev.SourceGenerators.OptionsBindingGeneration;
using Microsoft.CodeAnalysis;

namespace AStarDev.SourceGenerators.TestsUnit.OptionsBindingGeneration;

public sealed class GivenAnOptionsTypeInfo
{
    private static OptionsTypeInfo Create(string typeName = "MyOptions", string fullTypeName = "Ns.MyOptions", string sectionName = "Section")
        => new(typeName, fullTypeName, sectionName, Location.None);

    [Fact]
    public void when_all_values_match_then_the_instances_are_equal_and_share_a_hash_code()
    {
        var first = Create();
        var second = Create();

        first.Equals(second).ShouldBeTrue();
        first.Equals((object)second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Theory]
    [InlineData("Other", "Ns.MyOptions", "Section")]
    [InlineData("MyOptions", "Ns.Other", "Section")]
    [InlineData("MyOptions", "Ns.MyOptions", "Other")]
    [InlineData("MyOptions", "Ns.MyOptions", "section")]
    public void when_any_value_differs_then_the_instances_are_not_equal(string typeName, string fullTypeName, string sectionName)
        => Create().Equals(Create(typeName, fullTypeName, sectionName)).ShouldBeFalse();

    [Fact]
    public void when_only_the_generic_flag_differs_then_the_instances_are_not_equal()
        => Create().Equals(new OptionsTypeInfo("MyOptions", "Ns.MyOptions", "Section", Location.None, isGeneric: true)).ShouldBeFalse();

    [Fact]
    public void when_no_generic_flag_is_given_then_the_type_is_not_generic() => Create().IsGeneric.ShouldBeFalse();

    [Fact]
    public void when_compared_with_null_then_it_is_not_equal() => Create().Equals((object)null!).ShouldBeFalse();

    [Fact]
    public void when_compared_with_an_object_of_another_type_then_it_is_not_equal() => Create().Equals("Ns.MyOptions").ShouldBeFalse();

    [Fact]
    public void when_compared_with_itself_then_it_is_equal()
    {
        var info = Create();

        info.Equals(info).ShouldBeTrue();
    }

    [Fact]
    public void when_the_type_names_are_null_then_they_default_to_empty_strings()
    {
        var info = new OptionsTypeInfo(null!, null!, "Section", Location.None);

        info.TypeName.ShouldBeEmpty();
        info.FullTypeName.ShouldBeEmpty();
    }

    [Fact]
    public void when_converted_to_a_string_then_the_full_type_name_and_section_are_shown() => Create().ToString().ShouldBe("Ns.MyOptions (Section: Section)");
}
