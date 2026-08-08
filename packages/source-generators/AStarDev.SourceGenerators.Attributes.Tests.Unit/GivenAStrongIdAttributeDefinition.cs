namespace AStarDev.SourceGenerators.Attributes.Tests.Unit;

public sealed class GivenAStrongIdAttributeDefinition
{
    [Fact]
    public void when_constructed_with_no_id_type_then_id_type_defaults_to_guid()
        => new StrongIdAttribute().IdType.ShouldBe(typeof(Guid));

    [Fact]
    public void when_constructed_with_an_id_type_then_id_type_is_set()
        => new StrongIdAttribute(typeof(int)).IdType.ShouldBe(typeof(int));

    [Fact]
    public void when_attribute_usage_is_inspected_then_it_is_valid_on_structs_only()
        => typeof(StrongIdAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Struct);

    [Fact]
    public void when_attribute_usage_is_inspected_then_it_is_not_inherited()
        => typeof(StrongIdAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void when_attribute_usage_is_inspected_then_multiple_usage_is_not_allowed()
        => typeof(StrongIdAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
