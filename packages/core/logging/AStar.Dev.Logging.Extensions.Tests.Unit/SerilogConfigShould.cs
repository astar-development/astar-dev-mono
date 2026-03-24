using AStar.Dev.Logging.Extensions.Models;
using AStar.Dev.Utilities;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

public sealed class SerilogConfigShould
{
    [Fact]
    public void ContainTheExpectedProperties() =>
        new SerilogConfig().ToJson().ShouldMatchApproved();
}