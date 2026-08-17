using AStarDev.Utilities;
using JetBrains.Annotations;

namespace AStar.Dev.Api.HealthChecks;

[TestSubject(typeof(HealthStatusResponse))]
public class GivenAHealthStatusResponse
{
    [Fact]
    public void when_serialized_to_json_then_matches_approved_snapshot() =>
        new HealthStatusResponse
        {
            Name = "Test Name",
            Description = "Test Description",
            DurationInMilliseconds = 123,
            Data = new Dictionary<string, object>(),
            Exception = "Test Exception",
            Status = "OK"
        }
            .ToJson()
            .ShouldMatchApproved();
}
