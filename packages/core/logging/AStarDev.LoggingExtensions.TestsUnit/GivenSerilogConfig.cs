using AStar.Dev.Logging.Extensions.Models;
using AStarDev.Utilities;

namespace AStar.Dev.Logging.Extensions.TestsUnit;

[TestSubject(typeof(SerilogConfig))]
public sealed class GivenSerilogConfig
{
    [Fact]
    public void ContainTheExpectedProperties() =>
        new SerilogConfig().ToJson().ShouldMatchApproved();
}
