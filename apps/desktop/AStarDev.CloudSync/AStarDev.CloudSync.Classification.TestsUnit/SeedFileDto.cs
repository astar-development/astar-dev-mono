namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed record SeedFileDto(string Name, string Version, IReadOnlyList<string> Categories, IReadOnlyList<SeedTermDto> Terms);

public sealed record SeedTermDto(string Text, string Category);
