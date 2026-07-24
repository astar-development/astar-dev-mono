namespace AStar.Dev.Source.Generators.Attributes.Tests.Unit;

public sealed class StrongIdAttributeShould
{
    [Fact]
    public void DefaultToGuidWhenNoTypeIsSpecified()
        => new StrongIdAttribute().IdType.ShouldBe(typeof(Guid));

    [Fact]
    public void AllowSettingIdTypeViaConstructor()
        => new StrongIdAttribute(typeof(int)).IdType.ShouldBe(typeof(int));

    [Fact]
    public void BeApplicableToStructsOnly()
        => typeof(StrongIdAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Struct);

    [Fact]
    public void NotBeInherited()
        => typeof(StrongIdAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void NotAllowMultipleUsageOnSameStruct()
        => typeof(StrongIdAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
