namespace AStarDev.SourceGeneratorAttributes.TestsUnit;

public sealed class GivenARegisterServiceAttributeDefinition
{
    [Fact]
    public void when_constructed_with_no_lifetime_then_lifetime_defaults_to_scoped()
        => new AutoRegisterServiceAttribute().Lifetime.ShouldBe(ServiceLifetime.Scoped);

    [Fact]
    public void when_constructed_with_a_lifetime_then_lifetime_is_set()
        => new AutoRegisterServiceAttribute(ServiceLifetime.Singleton).Lifetime.ShouldBe(ServiceLifetime.Singleton);

    [Fact]
    public void when_as_property_is_set_then_as_is_returned()
        => new AutoRegisterServiceAttribute { As = typeof(string) }.As.ShouldBe(typeof(string));

    [Fact]
    public void when_as_self_property_is_set_then_as_self_is_true()
        => new AutoRegisterServiceAttribute { AsSelf = true }.AsSelf.ShouldBeTrue();

    [Fact]
    public void when_attribute_usage_is_inspected_then_it_is_valid_on_classes_only()
        => typeof(AutoRegisterServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Class);

    [Fact]
    public void when_attribute_usage_is_inspected_then_it_is_not_inherited()
        => typeof(AutoRegisterServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void when_attribute_usage_is_inspected_then_multiple_usage_is_not_allowed()
        => typeof(AutoRegisterServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
