namespace AStar.Dev.Source.Generators.Attributes.Tests.Unit;

public class AutoRegisterOptionsAttributeShould
{
    [Fact]
    public void SetSectionNameViaConstructor()
        => new AutoRegisterOptionsAttribute("MySection").SectionName.ShouldBe("MySection");

    [Fact]
    public void AllowSectionNameToBeNull()
        => new AutoRegisterOptionsAttribute(null).SectionName.ShouldBeNull();

    [Fact]
    public void BeApplicableToClassesAndStructs()
        => typeof(AutoRegisterOptionsAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Class | AttributeTargets.Struct);

    [Fact]
    public void NotBeInherited()
        => typeof(AutoRegisterOptionsAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void NotAllowMultipleUsageOnSameType()
        => typeof(AutoRegisterOptionsAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
