using AStar.Dev.Logging.Extensions.Models;
using AStar.Dev.Utilities;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

[TestSubject(typeof(SerilogConfig))]
public sealed class GivenSerilogConfig
{
    [Fact]
    public void ContainTheExpectedProperties() =>
        new SerilogConfig().ToJson().ShouldMatchApproved();
}
