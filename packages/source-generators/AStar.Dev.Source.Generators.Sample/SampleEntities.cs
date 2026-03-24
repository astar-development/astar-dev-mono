using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.Source.Generators.Sample;

/// <summary>
///
/// </summary>
[StrongId]
public partial record struct UserId;

/// <summary>
///
/// </summary>
[StrongId(typeof(int))]
public partial record struct UserId1;

/// <summary>
///
/// </summary>
[StrongId(typeof(string))]
public partial record struct UserId2;

/// <summary>
///
/// </summary>
[StrongId(typeof(Guid))]
public partial record struct UserId3;
