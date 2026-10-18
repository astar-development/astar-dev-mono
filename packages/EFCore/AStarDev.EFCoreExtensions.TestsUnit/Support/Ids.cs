using AStarDev.EFCoreExtensions;

namespace AStarDev.EFCoreExtensions.TestsUnit.Support;

internal sealed record IntId(int Value) : IStronglyTypedIntId<IntId>
{
    public static IntId Empty => new(0);
    public static IntId Create(int value) => new(value);
}

internal sealed record LongId(long Value) : IStronglyTypedLongId<LongId>
{
    public static LongId Empty => new(0);
    public static LongId Create(long value) => new(value);
}

internal sealed record GuidId(Guid Value) : IStronglyTypedGuidId<GuidId>
{
    public static GuidId Empty => new(Guid.Empty);
    public static GuidId Create(Guid value) => new(value);
}

internal sealed record StringId(string Value) : IStronglyTypedStringId<StringId>
{
    public static StringId Empty => new(string.Empty);
    public static StringId Create(string value) => new(value);
}
