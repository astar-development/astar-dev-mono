using AStarDev.EFCoreExtensions.TestsUnit.Support;

namespace AStarDev.EFCoreExtensions.TestsUnit;

public sealed class StronglyTypedIdConverter_should
{
    [Fact]
    public void convert_an_int_id_to_its_value()
    {
        var converter = new StronglyTypedIdConverter<IntId, int>();

        converter.ConvertToProvider(new IntId(42)).ShouldBe(42);
    }

    [Fact]
    public void convert_a_value_back_to_an_int_id()
    {
        var converter = new StronglyTypedIdConverter<IntId, int>();

        converter.ConvertFromProvider(42).ShouldBe(new IntId(42));
    }

    [Fact]
    public void round_trip_a_guid_id()
    {
        var converter = new StronglyTypedIdConverter<GuidId, Guid>();
        var id = new GuidId(Guid.NewGuid());

        var stored = converter.ConvertToProvider(id);

        stored.ShouldBe(id.Value);
        converter.ConvertFromProvider(stored).ShouldBe(id);
    }

    [Fact]
    public void round_trip_a_string_id()
    {
        var converter = new StronglyTypedIdConverter<StringId, string>();

        converter.ConvertFromProvider(converter.ConvertToProvider(new StringId("abc"))).ShouldBe(new StringId("abc"));
    }

    [Fact]
    public void expose_the_underlying_value_type_as_the_provider_type()
    {
        new StronglyTypedIdConverter<LongId, long>().ProviderClrType.ShouldBe(typeof(long));
    }
}
