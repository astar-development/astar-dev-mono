using AStar.Dev.Utilities;
using JetBrains.Annotations;

namespace AStar.Dev.Api.HealthChecks;

[TestSubject(typeof(HealthStatusResponse))]
public class HealthStatusResponseShould
{
    [Fact]
    public void ContainTheExpectedProperties() =>
        new HealthStatusResponse
            {
                Name                   = "Test Name",
                Description            = "Test Description",
                DurationInMilliseconds = 123,
                Data                   = new Dictionary<string, object>(),
                Exception              = "Test Exception",
                Status                 = "OK"
            }
            .ToJson()
            .ShouldMatchApproved();
}
