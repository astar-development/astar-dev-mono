namespace AStar.Dev.Source.Generators.Attributes.Tests.Unit;

public class RegisterServiceAttributeShould
{
    [Fact]
    public void HaveDefaultLifetimeAsScoped()
        => new AutoRegisterServiceAttribute().Lifetime.ShouldBe(ServiceLifetime.Scoped);

    [Fact]
    public void AllowSettingLifetimeViaConstructor()
        => new AutoRegisterServiceAttribute(ServiceLifetime.Singleton).Lifetime.ShouldBe(ServiceLifetime.Singleton);

    [Fact]
    public void AllowSettingAsProperty()
        => new AutoRegisterServiceAttribute { As = typeof(string) }.As.ShouldBe(typeof(string));

    [Fact]
    public void AllowSettingAsSelfProperty()
        => new AutoRegisterServiceAttribute { AsSelf = true }.AsSelf.ShouldBeTrue();

    [Fact]
    public void BeApplicableToClassesOnly()
        => typeof(AutoRegisterServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Class);

    [Fact]
    public void NotBeInherited()
        => typeof(AutoRegisterServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void NotAllowMultipleUsageOnSameClass()
        => typeof(AutoRegisterServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
