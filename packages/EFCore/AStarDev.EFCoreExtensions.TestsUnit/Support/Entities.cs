namespace AStarDev.EFCoreExtensions.TestsUnit.Support;

internal sealed class IntEntity
{
    public IntId Id { get; set; } = IntId.Empty;
    public string Name { get; set; } = string.Empty;
}

internal sealed class LongEntity
{
    public LongId Id { get; set; } = LongId.Empty;
    public string Name { get; set; } = string.Empty;
}

internal sealed class GuidEntity
{
    public GuidId Id { get; set; } = GuidId.Empty;
    public string Name { get; set; } = string.Empty;
}

internal sealed class StringEntity
{
    public StringId Id { get; set; } = StringId.Empty;
    public string Name { get; set; } = string.Empty;
}
