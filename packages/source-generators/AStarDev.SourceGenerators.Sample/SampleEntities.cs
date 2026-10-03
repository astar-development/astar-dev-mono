using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.SourceGenerators.Sample;

/// <summary>
///
/// </summary>
[StrongType]
public partial record struct UserId;

/// <summary>
///
/// </summary>
[StrongType(typeof(int))]
public partial record struct UserId1;

/// <summary>
///
/// </summary>
[StrongType(typeof(string))]
public partial record struct UserId2;

/// <summary>
///
/// </summary>
[StrongType(typeof(Guid))]
public partial record struct UserId3;
