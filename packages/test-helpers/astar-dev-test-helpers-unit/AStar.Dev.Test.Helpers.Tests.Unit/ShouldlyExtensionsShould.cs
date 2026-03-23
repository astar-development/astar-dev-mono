using AStar.Dev.Test.Helpers.Unit;
using JetBrains.Annotations;

namespace AStar.Dev.Test.Helpers;

[TestSubject(typeof(ShouldlyExtensions))]
public class ShouldlyExtensionsShould
{
    [Fact]
    public void MatchTheApprovedFileMaskingTheDateAndGuid()
        => new MockClass().ShouldMatchMaskedApproved();
}