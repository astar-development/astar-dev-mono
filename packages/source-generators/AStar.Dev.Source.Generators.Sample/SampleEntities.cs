using System;
using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.Source.Generators.Sample;

[StrongId]
public partial record struct UserId;

[StrongId(typeof(int))]
public partial record struct UserId1;

[StrongId(typeof(string))]
public partial record struct UserId2;

[StrongId(typeof(Guid))]
public partial record struct UserId3;
