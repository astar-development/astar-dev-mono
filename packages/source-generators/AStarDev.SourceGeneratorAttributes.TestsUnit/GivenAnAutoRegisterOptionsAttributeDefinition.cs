namespace AStarDev.SourceGeneratorAttributes.TestsUnit;

public sealed class GivenAnAutoRegisterOptionsAttributeDefinition
{
    [Fact]
    public void when_constructed_with_a_section_name_then_section_name_is_set()
        => new AutoRegisterOptionsAttribute("MySection").SectionName.ShouldBe("MySection");

    [Fact]
    public void when_constructed_with_a_null_section_name_then_section_name_is_null()
        => new AutoRegisterOptionsAttribute(null).SectionName.ShouldBeNull();

    [Fact]
    public void when_attribute_usage_is_inspected_then_it_is_valid_on_classes_and_structs()
        => typeof(AutoRegisterOptionsAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Class | AttributeTargets.Struct);

    [Fact]
    public void when_attribute_usage_is_inspected_then_it_is_not_inherited()
        => typeof(AutoRegisterOptionsAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void when_attribute_usage_is_inspected_then_multiple_usage_is_not_allowed()
        => typeof(AutoRegisterOptionsAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
