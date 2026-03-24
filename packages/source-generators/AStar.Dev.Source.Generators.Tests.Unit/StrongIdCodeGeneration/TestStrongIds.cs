using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.Source.Generators.Tests.Unit.StrongIdCodeGeneration;

// Test StrongId types used by the unit tests
[StrongId(typeof(string))]
internal readonly partial record struct UserId;

[StrongId(typeof(int))]
internal readonly partial record struct OrderId2;

[StrongId(typeof(System.Guid))]
internal readonly partial record struct EntityId;
